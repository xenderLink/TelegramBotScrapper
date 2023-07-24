using BotSpace;
using VacScrapper;

class Program
{
    static async Task Main(string[] args)
    {
        Bot bot = new ();
        Scrapper scrapper = new ();

        Thread scrapperThread = new Thread(scrapper.Start)
        {
            IsBackground = true,
            Name = "Scrapper"
        };
        
        scrapperThread.Start();
        
        await bot.Start();
    }
}