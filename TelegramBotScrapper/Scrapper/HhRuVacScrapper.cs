using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium; 
using OpenQA.Selenium.Chrome;
using SeleniumExtras.WaitHelpers;
using OpenQA.Selenium.Support.UI;
using VacancyModel;
using JsonCrud;

namespace Scrapper;
/// <summary>
/// Класс для сборки вакансий по тэгам, селекторам и xpath
/// Иногда этот класс будет переписываться в виду того, что разработчики hh.ru будут менять 
/// код в JS-скриптах, которые генерируют DOM-страницы.  
/// </summary>
public sealed class HhRuVacScrapper : Scrapper
{
    private readonly ILogger logger;
    private IWebDriver driver;
    private WebDriverWait wait;
    private ChromeOptions options;
    
    private readonly int[] milliseconds = { 3000, 4000, 5000, 6000, 7000 };

    private Dictionary<string, Vacancy> Vacancies;
    private string[] cities = { "Челябинск", "Екатеринбург", "Москва", "Санкт-Петербург" };
    private StringBuilder url;

    private int failsCount = 3;

    public HhRuVacScrapper(ILogger<Scrapper> lggr) => (logger, url) = (lggr, new StringBuilder());

    protected async override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                Vacancies = new ();
                options = new ();
                options.AddArguments(new string [] {
                        "--headless",
                        "--whitelisted-ips=\"\"",
                        "--disable-dev-shm-usage",
                        "--no-sandbox",
                           UserAgent,
                        "--window-size=1920,1050",
                        "--disable-gpu",
                        "--disable-logging",
                        "--disable-blink-features=AutomationControlled" });

                driver = new ChromeDriver(".", options, TimeSpan.FromMinutes(3));
                
                wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                wait.PollingInterval = TimeSpan.FromMilliseconds(200);

                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

                if (await DriverIsNavigated(cancellationToken) is false)
                {
                    if (failsCount is 0)
                    {
                        failsCount = 3;
                        await Task.Delay(TimeSpan.FromMinutes(30), cancellationToken);
                    }
                    continue;
                }
                
                if (await IsElementPresent(cancellationToken) is false)
                    continue;
                
                else
                {
                    JsonVacancy vacancies = new ();
                    await vacancies.Add(Vacancies);
                }

                await Task.Delay(TimeSpan.FromHours(7), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogError("Парсер прерван");
        }
    }

    private async Task<bool> IsElementPresent(CancellationToken cancellationToken)
    {
        bool elementsAreOk = true;
        
        try
        {
            var isItYourRegion = driver.FindElement(By.XPath("//button[@class='bloko-button bloko-button_kind-primary bloko-button_scale-small']"));

            wait.Until(ExpectedConditions.ElementToBeClickable(isItYourRegion)).Click();

            Func<string> inputText = delegate{

                string search = string.Empty;

                switch (rand.Next(1, 4))
                {
                    case 1:
                        search = "C# Developer";
                        break;

                    case 2:
                        search = "C# Разработчик";
                        break;

                    case 3:
                        search = "ASP NET";
                        break;
                }

                return search;
            };

            driver.FindElement(By.CssSelector("input[data-qa='search-input']")).SendKeys(inputText());
            await Task.Delay(milliseconds[rand.Next(0, 3)], cancellationToken);

            driver.FindElement(By.CssSelector("button[data-qa='search-button']")).Click();    
            await Task.Delay(milliseconds[rand.Next(0, 3)], cancellationToken);

            var uncheckElmt = driver.FindElement(By.XPath("//legend[text()='Регион']"))
                                    .FindElement(By.XPath("./../../.."))
                                    .FindElement(By.CssSelector("span[data-qa='serp__novafilter-title']"))
                                    .FindElement(By.XPath("./.."));
            
        
            wait.Until(ExpectedConditions.ElementToBeClickable(uncheckElmt));
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click()", uncheckElmt);
            await Task.Delay(milliseconds[rand.Next(0, 3)], cancellationToken);

            driver.FindElement(By.XPath("//button[@class='bloko-link bloko-link_pseudo' and text()='Показать все']")).Click();
            await Task.Delay(milliseconds[rand.Next(0, 3)], cancellationToken);

            var region = driver.FindElement(By.XPath("//input[@placeholder='Поиск региона']"));
            
            for (int i = 0; i < cities.Length; i++)
            {
                if (i > 0)
                {
                    try
                    {
                        region.Clear();
                    }
                    catch (StaleElementReferenceException)
                    {
                        driver.FindElement(By.XPath("//button[@class='bloko-link bloko-link_pseudo' and text()='Показать все']")).Click();
                        await Task.Delay(milliseconds[rand.Next(0, 3)], cancellationToken);

                        region = driver.FindElement(By.XPath("//input[@placeholder='Поиск региона']"));
                    }
                }

                region.SendKeys(cities[i]);
                await Task.Delay(milliseconds[rand.Next(0, 3)], cancellationToken);

                var checkedCity = driver.FindElement(By.XPath($"//span[@data-qa='serp__novafilter-title' and text()='{cities[i]}']"))
                                        .FindElement(By.XPath("./.."));
                
                checkedCity.Click();
                await Task.Delay(milliseconds[rand.Next(2, 5)], cancellationToken);
            }

            do
            {
                var vacancyElements = driver.FindElement(By.CssSelector("main[class='vacancy-serp-content']"))
                                            .FindElements(By.XPath("//div[@class='vacancy-serp-item__layout']")); // блоки с вакансиями

                foreach (var vacancy in vacancyElements)  // перебор блоков и собирание информацию с каждого из них
                {
                    var anchor = vacancy.FindElement(By.TagName("a"));
                    url.Append(anchor.GetAttribute("href"));

                    var id = Regex.Match(url.ToString(), @"(?<=vacancy/)([0-9]+)").Value;

                    var city = vacancy.FindElement(By.CssSelector("div[data-qa='vacancy-serp__vacancy-address']"));
        
                    if (Vacancies.ContainsKey(id) is true) // Проверка на наличие вакансии в словаре
                    {
                        url.Clear();
                        continue;                        
                    }
                    
                    Vacancies.Add(id, new Vacancy(anchor.Text, url.ToString(), city.Text));

                    url.Clear();
                }                
 
                if (await NextButtonExists("//span[text()='дальше']", cancellationToken) is false) // кнопка "Дальше" и если её нет, то выйти из цикла по перебору страниц
                    break;
                
                
                await Task.Delay(milliseconds[rand.Next(0, 3)], cancellationToken);
                
            } while (true);           
        }
        catch (OperationCanceledException)
        {
            logger.LogError("Парсер прерван");
        }
        catch (ElementClickInterceptedException)
        {
            logger.LogError($"Проблема при работе с элементами. Возможно, структура сайта изменена. Перезапуск парсера");

            elementsAreOk = false;
        }
        catch (Exception)
        {
            logger.LogError($"Проблема при работе с элементами. Возможно, структура сайта изменена. Перезапуск парсера.");

            elementsAreOk = false;
        }
        finally
        {
            driver.Dispose();
        }

        return elementsAreOk;
    }

    private async Task<bool> DriverIsNavigated(CancellationToken cancellationToken)
    {
        bool driverIsStarted = true;
        try 
        {
            driver.Navigate().GoToUrl("https://hh.ru/");
        }
        catch (OperationCanceledException)
        {
            logger.LogError("Парсер прерван");
            driver.Dispose();
            
            await Task.Delay(1, cancellationToken);
        }
        catch (WebDriverException)
        {
            logger.LogError($"Ошибка запуска парсера. После {failsCount} раз(а), запуск парсера будет через 30 минут.");
            
            failsCount--;
            driverIsStarted = false; 
            driver.Dispose();
        }

        return driverIsStarted;
    }

    public async Task<bool> NextButtonExists (string element, CancellationToken cancellationToken)
    {
        bool isThereNextButton = true;
        try
        {
            var nextButton = driver.FindElement(By.XPath(element)).FindElement(By.XPath("./..")); // ищем кнопку

            wait.Until(ExpectedConditions.ElementToBeClickable(nextButton)).Click();              // когда нашли, то ждём, чтобы она стала "нажимаемой"
        }
        catch (OperationCanceledException)
        {
            logger.LogError("Парсер прерван");
            await Task.Delay(1, cancellationToken);
        }
        catch (NoSuchElementException)
        {
            isThereNextButton = false;
        }

        return isThereNextButton;
    }
}