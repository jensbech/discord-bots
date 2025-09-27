using DiscordBots.Core;
using Microsoft.Extensions.Hosting;

namespace DiscordBots.BoredBot;

public static class Program
{
    public static async Task Main(string[] args)
    {
        using var host = DiscordBotHostingExtensions.CreateDiscordBotHost<BoredBot>(
            args,
            (envVars, commands, logger) => BoredBot.GetInstanceAsync(envVars, commands, logger),
            BoredBotCommands.Commands,
            "Bored Bot"
        );
        await host.RunAsync();
    }
}
