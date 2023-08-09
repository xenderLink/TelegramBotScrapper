using BotSpace;
using VacScrapper;

class Program
{
    static async Task Main()
    {
        Bot bot = new ();
        Scrapper scrapper = new ();

        Thread scrapperThread = new (scrapper.Start)
        {
            IsBackground = true,
            Name = "Scrapper"
        };
        
        scrapperThread.Start();
        
        await bot.Start();
    }
}