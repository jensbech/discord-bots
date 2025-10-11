namespace DiscordBots.OpenAI;

public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAI";
    public string ApiKey { get; set; } = string.Empty;
    public string? Project { get; set; }
    public string Model { get; set; } = "gpt-4.1";
}
