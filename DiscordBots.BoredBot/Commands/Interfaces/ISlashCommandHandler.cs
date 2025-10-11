using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace DiscordBots.BoredBot.Commands.Interfaces;

public interface ISlashCommandHandler
{
    string Name { get; }
    Task HandleAsync(SocketSlashCommand command, ILogger logger);
}
