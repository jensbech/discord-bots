using Discord;
using Discord.WebSocket;
using DiscordBots.BookStack.Interfaces;
using DiscordBots.BoredBot.Commands.Handlers;
using DiscordBots.BoredBot.Commands.Interfaces;
using DiscordBots.Core;
using DiscordBots.Core.Logging;
using DiscordBots.OpenAI.Interfaces;
using Microsoft.Extensions.Logging;

namespace DiscordBots.BoredBot
{
    public abstract class BoredBot(
        string token,
        SlashCommandBuilder[] commands,
        ILogger<BoredBot> logger,
        IBookStackClient bookStackClient,
        IOpenAiClient openAiClient
    ) : BaseDiscordBot(token, commands, logger)
    {
        private readonly IReadOnlyDictionary<string, ISlashCommandHandler> _handlers = new List<ISlashCommandHandler>
        {
            new Roll(),
            new Search(bookStackClient),
            new Chat(openAiClient),
            new Ask(bookStackClient, openAiClient),
            new Help(),
        }.ToDictionary(h => h.Name, StringComparer.OrdinalIgnoreCase);

        protected override async Task OnSlashCommandAsync(SocketSlashCommand command)
        {
            if (_handlers.TryGetValue(command.CommandName, out var handler))
            {
                await handler.HandleAsync(command, Logger);
            }
            else
            {
                await command.RespondAsync("Unknown command.", ephemeral: true);
                Logger.LogSlashError(command, "Unhandled command");
            }
        }
    }
}
