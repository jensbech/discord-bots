using Discord.WebSocket;
using DiscordBots.BoredBot.Dice;
using Microsoft.Extensions.Logging;

namespace DiscordBots.BoredBot.Commands;

internal sealed class RollHandler : ISlashCommandHandler
{
    public string Name => "roll";

    public async Task HandleAsync(SocketSlashCommand command, ILogger logger)
    {
        var userInput =
            command.Data.Options?.FirstOrDefault(o => o.Name == "input")?.Value?.ToString()
            ?? string.Empty;
        if (Roller.TryHandleRollCommand(userInput, out var resultMessage))
        {
            await command.RespondAsync(resultMessage);
            logger.LogInformation("/roll {Input} => {Result}", userInput, resultMessage);
        }
        else
        {
            await command.RespondAsync(resultMessage, ephemeral: true);
            logger.LogWarning("/roll error {Error}", resultMessage);
        }
    }
}
