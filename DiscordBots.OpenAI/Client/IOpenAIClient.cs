using DiscordBots.OpenAI.Models;

namespace DiscordBots.OpenAI;

public interface IOpenAIClient
{
    Task<string?> ChatAboutDndRulesAsync(string question, CancellationToken ct = default);

    Task<string?> ChatWithContextAsync(
        string question,
        IReadOnlyList<string> documents,
        CancellationToken ct = default
    );
}
