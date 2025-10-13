using Discord;

namespace DiscordBots.Scribe.Commands
{
    public static class CommandBuilders
    {
        public static SlashCommandBuilder[] Commands =>
            [
                new SlashCommandBuilder()
                    .WithName("roll")
                    .WithDescription("Roll dice (e.g. 'd20', '6d12-4', '2d8 + 1d6+4')")
                    .AddOption(
                        name: "input",
                        ApplicationCommandOptionType.String,
                        description: "The dice you want to roll",
                        isRequired: true
                    ),
                new SlashCommandBuilder()
                    .WithName("rules")
                    .WithDescription("Ask questions about D&D 5E rules")
                    .AddOption(
                        name: "question",
                        type: ApplicationCommandOptionType.String,
                        description: "Your D&D rules question",
                        isRequired: true
                    ),
                new SlashCommandBuilder()
                    .WithName("ask")
                    .WithDescription("Ask any question answered from the Wiki contents")
                    .AddOption(
                        name: "question",
                        type: ApplicationCommandOptionType.String,
                        description: "Your knowledge-base question",
                        isRequired: true
                    ),
                new SlashCommandBuilder()
                    .WithName("help")
                    .WithDescription("Displays a list of available commands."),
            ];
    }
}
