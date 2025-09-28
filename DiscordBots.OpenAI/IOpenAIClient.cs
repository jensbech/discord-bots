using DiscordBots.OpenAI.Models;

namespace DiscordBots.OpenAI;

public interface IOpenAIClient
{
    Task<string?> ChatAsync(string question, CancellationToken ct = default);
}
