using Discord;
using Discord.WebSocket;
using DiscordBots.BookStack.Interfaces;
using DiscordBots.Core;
using DiscordBots.Core.Logging;
using DiscordBots.OpenAI.Interfaces;
using DiscordBots.Scribe.Commands.Handlers;
using DiscordBots.Scribe.Commands.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiscordBots.Scribe;

public class ScribeBotBase(
    IOptions<ScribeBotOptions> options,
    SlashCommandBuilder[] commands,
    ILogger<ScribeBotBase> logger,
    IBookStackClient bookStackClient,
    IOpenAiClient openAiClient
) : BaseDiscordBot(options.Value.Token, options.Value.BotName, commands, logger)
{
    private readonly IReadOnlyDictionary<string, ISlashCommandHandler> _handlers =
        new List<ISlashCommandHandler>
        {
            new Roll(),
            new Rules(openAiClient),
            new Ask(bookStackClient, openAiClient),
            new Tldr(openAiClient),
            new Help(),
        }.ToDictionary(h => h.Name, StringComparer.OrdinalIgnoreCase);

    protected override async Task OnSlashCommandAsync(SocketSlashCommand command)
    {
        if (_handlers.TryGetValue(command.CommandName, out var handler))
        {
            await handler.HandleAsync(command, logger);
        }
        else
        {
            await command.RespondAsync("Unknown command.", ephemeral: true);
            logger.LogSlashError(command, "Unhandled command");
        }
    }
}
