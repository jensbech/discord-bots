namespace DiscordBots.OpenAI;

public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAI";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4.1";
    public int MaxTokens { get; set; } = 1500;
    public string? Project { get; set; }
}
