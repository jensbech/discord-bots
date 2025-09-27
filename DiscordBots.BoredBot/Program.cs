using DiscordBots.BoredBot;
using DiscordBots.Core;

var environmentVariables = DiscordBot.EnsureEnvironmentVariables();
_ = await BoredBot.GetInstanceAsync(environmentVariables, BoredBotCommands.Commands);
