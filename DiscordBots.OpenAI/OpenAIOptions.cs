namespace DiscordBots.OpenAI;

public sealed class OpenAIOptions
{
    public const string SectionName = "OpenAI";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-3.5-turbo";
    public string BaseUrl { get; set; } = "https://api.openai.com";
    public int MaxTokens { get; set; } = 1000;
}
