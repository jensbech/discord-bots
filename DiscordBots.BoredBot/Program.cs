using DiscordBots.BoredBot;
using DiscordBots.Core;

DiscordBot.VerifyEnvironmentVariables(["APPLICATION_ID", "DISCORD_BOT_TOKEN"]);

await BoredBot.GetInstanceAsync(
    Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN")!,
    Environment.GetEnvironmentVariable("APPLICATION_ID")!,
    BoredBotCommands.Commands);