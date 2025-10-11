using Discord.WebSocket;
using DiscordBots.BoredBot.Commands.Interfaces;
using Microsoft.Extensions.Logging;

namespace DiscordBots.BoredBot.Commands.Handlers;

internal sealed class Help : ISlashCommandHandler
{
    public string Name => "help";

    public async Task HandleAsync(SocketSlashCommand command, ILogger logger)
    {
        await command.RespondAsync("Available commands: /roll, /search, /chat, /ask, /help");
        logger.LogInformation("/help invoked");
    }
}
