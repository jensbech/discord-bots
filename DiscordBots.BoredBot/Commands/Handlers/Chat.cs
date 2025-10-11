using Discord.WebSocket;
using DiscordBots.BoredBot.Commands.Interfaces;
using DiscordBots.OpenAI.Interfaces;
using Microsoft.Extensions.Logging;

namespace DiscordBots.BoredBot.Commands.Handlers;

internal sealed class Chat(IOpenAiClient openAiClient) : ISlashCommandHandler
{
    public string Name => "chat";

    public async Task HandleAsync(SocketSlashCommand command, ILogger logger)
    {
        var question =
            command.Data.Options?.FirstOrDefault(o => o.Name == "question")?.Value?.ToString()
            ?? string.Empty;

        await command.DeferAsync();

        var response = await openAiClient.RulesChat(question);
        await command.FollowupAsync(response);
        logger.LogInformation("/chat answered");
    }
}
