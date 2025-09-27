using DiscordBots.Core;
using Microsoft.Extensions.Hosting;

namespace DiscordBots.BoredBot;

public static class Program
{
    public static async Task Main(string[] args)
    {
        using var host = DiscordBotHostingExtensions.DiscordBotBuilder<BoredBot>(
            args,
            BoredBot.GetOrCreateInstance,
            BoredBotCommands.Commands,
            "Bored Bot"
        );
        await host.RunAsync();
    }
}
