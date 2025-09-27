using DiscordBots.BoredBot;

if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN")) || 
    string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATION_ID")))
{
    throw new InvalidOperationException("DISCORD_BOT_TOKEN and APPLICATION_ID must be defined in the environment variables.");
}

await BoredBot.GetInstanceAsync(
    Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN")!,
    Environment.GetEnvironmentVariable("APPLICATION_ID")!,
    BoredBotCommands.Commands);