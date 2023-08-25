using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using TelegramBotScrapper.Helpers;
using BotScrapper;
using Scrapper;

class Program
{
    static async Task Main(string[] args)
    {
        using CancellationTokenSource cts = new ();
        
        var appBuilder = Host.CreateApplicationBuilder(args);

        appBuilder.Services.AddHostedService<Bot>();
        appBuilder.Services.AddHostedService<HhRuVacScrapper>();
        appBuilder.Services.AddSingleton<IHhRuVacancySender, HhRuVacancySender>();
        
        var app = appBuilder.Build();

        await app.RunAsync(cts.Token);
    }
}