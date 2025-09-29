namespace DiscordBots.OpenAI;

public interface IOpenAIClient
{
    Task<string?> ChatAsync(string question);
    Task<string?> ChatWithContextAsync(string question, IReadOnlyList<string> documents);
}
