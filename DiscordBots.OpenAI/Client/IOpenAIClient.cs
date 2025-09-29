using DiscordBots.OpenAI.Models;

namespace DiscordBots.OpenAI;

public interface IOpenAIClient
{
    Task<string?> ChatAsync(string question, CancellationToken ct = default);

    Task<string?> ChatWithContextAsync(
        string question,
        IReadOnlyList<string> documents,
        CancellationToken ct = default
    );
}
