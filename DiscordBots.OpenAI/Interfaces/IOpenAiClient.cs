namespace DiscordBots.OpenAI.Interfaces;

public interface IOpenAiClient
{
    Task<string?> RulesChat(string question);
    Task<string?> AskChat(string question, IReadOnlyList<string> documents);
    Task<string?> ReduceSearchQuery(string question);
}
