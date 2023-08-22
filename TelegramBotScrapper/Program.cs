﻿using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using BotSpace;
using Scrapper;

class Program
{
    static async Task Main(string[] args)
    {
        using CancellationTokenSource cts = new ();
        
        HostApplicationBuilder botBuilder = Host.CreateApplicationBuilder(args);

        var appBuilder = Host.CreateDefaultBuilder(args).ConfigureServices(bldr =>
            {
                bldr.AddHostedService<Bot>();
                bldr.AddHostedService<HhRuVacScrapper>();
                bldr.Configure<HostOptions>(opts => opts.ShutdownTimeout = TimeSpan.FromSeconds(3));
            });
        
        var app = appBuilder.Build();

        await app.RunAsync(cts.Token);
    }
}