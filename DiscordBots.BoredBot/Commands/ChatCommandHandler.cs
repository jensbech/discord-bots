using System.Threading.Tasks;
using Discord.WebSocket;
using DiscordBots.OpenAI;
using Microsoft.Extensions.Logging;

namespace DiscordBots.BoredBot.Commands;

internal sealed class ChatCommandHandler : ISlashCommandHandler
{
    private readonly IOpenAIClient _openAIClient;

    public ChatCommandHandler(IOpenAIClient openAIClient) => _openAIClient = openAIClient;

    public string Name => "chat";

    public async Task HandleAsync(SocketSlashCommand command, ILogger logger)
    {
        var question =
            command.Data.Options?.FirstOrDefault(o => o.Name == "question")?.Value?.ToString()
            ?? string.Empty;
        await command.DeferAsync();
        var response = await _openAIClient.ChatAsync(question);
        await command.FollowupAsync(response);
        logger.LogInformation("/chat answered");
    }
}
