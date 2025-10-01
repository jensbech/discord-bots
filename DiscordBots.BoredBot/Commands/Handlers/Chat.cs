using Discord.WebSocket;
using DiscordBots.OpenAI;
using Microsoft.Extensions.Logging;

namespace DiscordBots.BoredBot.Commands.Handlers;

internal sealed class Chat(IOpenAIClient openAIClient) : ISlashCommandHandler
{
    private readonly IOpenAIClient _openAIClient = openAIClient;

    public string Name => "chat";

    public async Task HandleAsync(SocketSlashCommand command, ILogger logger)
    {
        var question =
            command.Data.Options?.FirstOrDefault(o => o.Name == "question")?.Value?.ToString()
            ?? string.Empty;
        await command.DeferAsync();
        var response = await _openAIClient.RulesChat(question);
        await command.FollowupAsync(response);
        logger.LogInformation("/chat answered");
    }
}
