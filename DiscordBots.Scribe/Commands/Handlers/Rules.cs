using Discord.WebSocket;
using DiscordBots.OpenAI.Interfaces;
using DiscordBots.Scribe.Commands.Interfaces;
using Microsoft.Extensions.Logging;

namespace DiscordBots.Scribe.Commands.Handlers;

internal sealed class Rules(IOpenAiClient openAiClient) : ISlashCommandHandler
{
    public string Name => "rules";

    public async Task HandleAsync(SocketSlashCommand command, ILogger logger)
    {
        var question =
            command.Data.Options?.FirstOrDefault(o => o.Name == "question")?.Value?.ToString()
            ?? string.Empty;

        await command.DeferAsync();

        var response = await openAiClient.RulesChat(question);
        await command.FollowupAsync(response);
        logger.LogInformation("/rules answered");
    }
}