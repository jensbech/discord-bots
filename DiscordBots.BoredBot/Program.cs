using DiscordBots.BookStack;
using DiscordBots.Core;
using DiscordBots.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DiscordBots.BoredBot;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Debug);

        builder.Services.AddBookStack(builder.Configuration);
        builder.Services.AddOpenAI(builder.Configuration);
        builder.Services.AddHostedService<ServiceInitializer>();

        builder.AddDiscordBot<BoredBot>(
            BoredBot.GetOrCreateInstance,
            CommandBuilders.Commands,
            "Bored Bot"
        );

        using var host = builder.Build();
        await host.RunAsync();
    }
}
