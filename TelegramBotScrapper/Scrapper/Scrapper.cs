using System.Text;
using System.Text.RegularExpressions;
using OpenQA.Selenium; 
using OpenQA.Selenium.Chrome;
using SeleniumExtras.WaitHelpers;
using OpenQA.Selenium.Support.UI;
using VacancyModel;
using JsonCrud;

namespace VacScrapper;
/// <summary>
/// Класс для сборки вакансий по тэгам, селекторам и xpath
/// Иногда этот класс будет переписываться в виду того, что разработчики hh.ru будут менять 
/// код в JS-скриптах, которые генерируют DOM-страницы.  
/// </summary>
public sealed class Scrapper
{
    private IWebDriver driver;
    private WebDriverWait wait;
    private ChromeOptions options;
    
    private Random rand;
    private readonly int[] milliseconds = { 3000, 4000, 5000, 6000, 7000 };

    private Dictionary<string, Vacancy> Vacancies = new ();
    private string[] cities = { "Челябинск", "Екатеринбург", "Москва", "Санкт-Петербург" };
    private StringBuilder url;

    private int failsCount = 3;

    public Scrapper()
    {
        url = new ();
        rand = new ();
    }

    public async void Start()
    {
        while (true)
        {
            options = new ();
            options.AddArguments(new string [] {
                    UserAgent,
                    // "--start-maximized",
                    "--window-size=1920,1050",
                    "--headless",
                    "--disable-logging",
                    "--no-sandbox",
                    "--disable-blink-features=AutomationControlled",
                    });

            driver = new ChromeDriver(".", options, TimeSpan.FromMinutes(3));
            wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            wait.PollingInterval = TimeSpan.FromMilliseconds(200);
            
            Thread.Sleep(5000);

            if (DriverIsNavigated() is false)
                continue;

            if (IsElementPresent() is false)
            {
                Console.WriteLine("Структура сайта поменялась. Нужно переписать парсер.");
            }
            else
            {
                JsonVacancy vacancies = new ();
                await vacancies.Add(Vacancies);
            }

            driver.Dispose();
            Thread.Sleep(TimeSpan.FromHours(7));
        }
    }

    private bool IsElementPresent()
    {
        try
        {
            driver.FindElement(By.XPath("//button[@class='bloko-button bloko-button_kind-primary bloko-button_scale-small']"))
                  .Click();

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

            driver.FindElement(By.CssSelector("input[data-qa='search-input']"))
                  .SendKeys(inputText());
            Thread.Sleep(milliseconds[rand.Next(0, 3)]);

            driver.FindElement(By.CssSelector("button[data-qa='search-button']"))
                  .Click();    
            Thread.Sleep(milliseconds[rand.Next(0, 3)]);

            var uncheckElmt = driver.FindElement(By.XPath("//legend[text()='Регион']"))
                                    .FindElement(By.XPath("./../../.."))
                                    .FindElement(By.CssSelector("span[data-qa='serp__novafilter-title']"))
                                    .FindElement(By.XPath("./.."));

            uncheckElmt.Click();
            Thread.Sleep(milliseconds[rand.Next(0, 3)]);

            driver.FindElement(By.XPath("//button[@class='bloko-link bloko-link_pseudo' and text()='Показать все']"))
                  .Click();
            Thread.Sleep(milliseconds[rand.Next(0, 3)]);

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
                        driver.FindElement(By.XPath("//button[@class='bloko-link bloko-link_pseudo' and text()='Показать все']"))
                              .Click();
                        Thread.Sleep(milliseconds[rand.Next(0, 3)]);

                        region = driver.FindElement(By.XPath("//input[@placeholder='Поиск региона']"));
                    }
                }

                region.SendKeys(cities[i]);
                Thread.Sleep(milliseconds[rand.Next(0, 3)]);

                var checkedCity = driver.FindElement(By.XPath($"//span[@data-qa='serp__novafilter-title' and text()='{cities[i]}']"))
                                        .FindElement(By.XPath("./.."));
                
                checkedCity.Click();
                Thread.Sleep(milliseconds[rand.Next(2, 5)]);
            }

            do
            {
                var vacancyElements = driver.FindElement(By.CssSelector("main[class='vacancy-serp-content']"))
                                            .FindElements(By.XPath("//div[@class='vacancy-serp-item__layout']")); // блоки с вакансиями

                foreach (var vacancy in vacancyElements)  // перебор блоков и собирание информацию с каждого из них
                {
                    var anchor = vacancy.FindElement(By.TagName("a"));
                    url.Append(anchor.GetAttribute("href"));

                    var id = (Regex.Match(url.ToString(), @"(?<=vacancy/)([0-9]+)").Value);

                    var city = vacancy.FindElement(By.CssSelector("div[data-qa='vacancy-serp__vacancy-address']"));
        
                    if (Vacancies.ContainsKey(id) is true) // Проверка на наличие вакансии в словаре
                    {
                        url.Clear();
                        continue;                        
                    }
                    
                    Vacancies.Add(id, new Vacancy(anchor.Text, url.ToString(), city.Text));

                    url.Clear();
                }                
 
                if (NextButtonExists("//span[text()='дальше']") is false) // кнопка "Дальше" и если её нет, то выйти из цикла по перебору страниц
                {
                     Thread.Sleep(milliseconds[rand.Next(0, 3)]);
                     break;
                }
                
                Thread.Sleep(milliseconds[rand.Next(0, 3)]);
                
            } while (true);
   
            return true;           
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }

    private bool DriverIsNavigated()
    {
        try 
        {
            driver.Navigate().GoToUrl("https://hh.ru/");

            return true;
        }
        catch (WebDriverException)
        {
            Console.WriteLine($"Ошибка запуска парсера. После {failsCount} раз(а), запуск парсера будет через 30 минут.");
            failsCount--;

            driver.Dispose();

            if (failsCount == 0)
            {
                failsCount = 3;
                Thread.Sleep(TimeSpan.FromMinutes(30));
            }

            return false;
        }
    }

    public bool NextButtonExists (string element)
    {
        try
        {
            var nextButton = driver.FindElement(By.XPath(element))
                                   .FindElement(By.XPath("./.."));                     // ищем кнопку

            wait.Until(ExpectedConditions.ElementToBeClickable(nextButton)).Click();   // когда нашли, то ждём, чтобы она стала "нажимаемой"
            
            return true;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }

    private string UserAgent
    {
        get
        {
            string agent = string.Empty;

            switch (rand.Next(1, 4))
            {
                case 1:
                    agent = "--user-agent=Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36";
                    break;

                case 2:
                    agent = "--user-agent=Mozilla/5.0 (X11; CrOS x86_64 10066.0.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36";
                    break;
                
                case 3:
                    agent = "--user-agent=Mozilla/5.0 (Macintosh; Intel Mac OS X 10_14_6) AppleWebKit/605.1.15 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/604.1 Edg/114.0.100.0";
                    break;
            }

            return agent;
        }
    }
}