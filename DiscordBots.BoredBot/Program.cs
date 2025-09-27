using DiscordBots.BoredBot;
using DiscordBots.Core;

await BoredBot.GetInstanceAsync(
    DiscordBot.EnsureEnvironmentVariables(),
    BoredBotCommands.Commands);