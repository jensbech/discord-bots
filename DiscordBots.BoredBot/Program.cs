using DiscordBots.BookStack;
using DiscordBots.Core;
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
        // Add BookStack (expects configuration section 'BookStack')
        builder.Services.AddBookStack(builder.Configuration);
        builder.Services.AddHostedService<BookStackBotInitializer>();
        // Add Discord bot
        builder.AddDiscordBot<BoredBot>(
            BoredBot.GetOrCreateInstance,
            BoredBotCommands.Commands,
            "Bored Bot"
        );

        using var host = builder.Build();
        await host.RunAsync();
    }
}
