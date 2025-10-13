using Discord.WebSocket;
using DiscordBots.Scribe.Commands.Interfaces;
using Microsoft.Extensions.Logging;

namespace DiscordBots.Scribe.Commands.Handlers;

internal sealed class Help : ISlashCommandHandler
{
    public string Name => "help";

    public async Task HandleAsync(SocketSlashCommand command, ILogger logger)
    {
        var helpMessage = """
        **Available Commands:**

        🎲 **/roll** `<dice>`
        Roll dice using standard notation (e.g., `d20`, `2d6+3`, `1d8+1d4`)

        📖 **/rules** `<question>`
        Ask questions about D&D 5E rules using AI assistance

        🤖 **/ask** `<question>`
        Ask any question answered from the Wiki contents

        ❓ **/help**
        Display this help message

        **Examples:**
        • `/roll 2d6+4` - Roll two six-sided dice and add 4
        • `/rules "How does advantage work?"` - Ask about D&D 5E advantage mechanics
        • `/ask "Who is Timou Grolsch?"` - Ask about any content from the DnD wiki
        """;

        await command.RespondAsync(helpMessage);
        logger.LogInformation("/help invoked");
    }
}
