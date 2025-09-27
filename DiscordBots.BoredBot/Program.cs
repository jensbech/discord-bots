using DiscordBots.BoredBot;
using DiscordBots.Core;
using Microsoft.Extensions.Hosting;

namespace DiscordBots.BoredBot;

public static class Program
{
    public static async Task Main(string[] args)
    {
        using var host = DiscordBotHostingExtensions.CreateDiscordBotHost<BoredBot>(
            args,
            BoredBot.GetInstanceAsync,
            BoredBotCommands.Commands,
            "Bored Bot"
        );

        await host.RunAsync();
    }
}
