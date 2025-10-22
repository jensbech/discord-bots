using System;
using System.Threading.Tasks;
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

    protected override Task OnSlashCommandAsync(SocketSlashCommand command)
    {
        if (_handlers.TryGetValue(command.CommandName, out var handler))
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await handler.HandleAsync(command, logger);
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Unhandled exception in async slash command handler for /{Command}",
                        command.CommandName
                    );

                    var friendly =
                        $"Something went wrong handling /{command.CommandName}. Please try again or use /help.";

                    if (!command.HasResponded)
                    {
                        try
                        {
                            await command.RespondAsync(friendly, ephemeral: true);
                        }
                        catch (Exception innerEx)
                        {
                            logger.LogError(
                                innerEx,
                                "Failed sending initial error response for /{Command}",
                                command.CommandName
                            );
                        }
                    }
                    else
                    {
                        try
                        {
                            await command.FollowupAsync(friendly, ephemeral: true);
                        }
                        catch (Exception innerEx)
                        {
                            logger.LogError(
                                innerEx,
                                "Failed sending followup error response for /{Command}",
                                command.CommandName
                            );
                        }
                    }
                }
            });
        }
        else
        {
            _ = command.RespondAsync("Unknown command.", ephemeral: true);
            logger.LogSlashError(command, "Unhandled command");
        }

        // Return immediately so the gateway thread is not blocked; each handler runs concurrently.
        return Task.CompletedTask;
    }
}
