namespace DiscordBots.OpenAI;

public sealed class OpenAIOptions
{
    public const string SectionName = "OpenAI";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4.1";
    public string BaseUrl { get; set; } = "https://api.openai.com";
    public int MaxTokens { get; set; } = 1500;
    public string? Organization { get; set; }
    public string? Project { get; set; }
}
