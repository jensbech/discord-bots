using Discord;

namespace DiscordBots.BoredBot
{
    public static class BoredBotCommands
    {
        public static SlashCommandBuilder[] Commands => new[]
        {
            new SlashCommandBuilder()
                .WithName("roll")
                .WithDescription("Roll dice (e.g. 'd20', '6d12-4', '2d8 + 1d6+4')")
                .AddOption("input", ApplicationCommandOptionType.String, "The dice you want to roll", isRequired: true),
            
            new SlashCommandBuilder()
                .WithName("help")
                .WithDescription("Displays a list of available commands.")
        };
    }
}