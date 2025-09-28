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

        // Add BookStack (expects configuration section 'BookStack')
        builder.Services.AddBookStack(builder.Configuration);
        // Add OpenAI (expects OPENAI_API_KEY env var)
        builder.Services.AddOpenAI(builder.Configuration);
        builder.Services.AddHostedService<ServiceBotInitializer>();

        builder.AddDiscordBot<BoredBot>(
            BoredBot.GetOrCreateInstance,
            BoredBotCommands.Commands,
            "Bored Bot"
        );

        using var host = builder.Build();
        await host.RunAsync();
    }
}
