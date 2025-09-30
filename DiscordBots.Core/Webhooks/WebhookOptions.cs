namespace DiscordBots.Core.Webhooks;

public sealed class WebhookOptions
{
    public const string SectionName = "Webhooks";
    public string[] AllowedSources { get; set; } = [];
    public bool EnableBookStack { get; set; } = true;
    public string? NewPostMessagesPath { get; set; } = "resources/new_post_messages.json";
    public ulong? NewPostChannelId { get; set; }
}
