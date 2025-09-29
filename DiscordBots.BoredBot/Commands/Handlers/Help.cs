using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace DiscordBots.BoredBot.Commands;

internal sealed class HelpHandler : ISlashCommandHandler
{
    public string Name => "help";

    public async Task HandleAsync(SocketSlashCommand command, ILogger logger)
    {
        await command.RespondAsync("Available commands: /roll, /search, /chat, /ask, /help");
        logger.LogInformation("/help invoked");
    }
}
