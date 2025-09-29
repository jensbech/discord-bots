namespace DiscordBots.OpenAI;

public sealed class OpenAIOptions
{
    public const string SectionName = "OpenAI";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-3.5-turbo-0125";
    public string BaseUrl { get; set; } = "https://api.openai.com";
    public int MaxTokens { get; set; } = 1000;
    public string? Organization { get; set; }
    public string? Project { get; set; }
}
