namespace DiscordBots.BookStack;

public sealed class BookStackOptions
{
    public const string SectionName = "BookStack";
    public required string ApiId { get; set; } = string.Empty;
    public required string ApiKey { get; set; } = string.Empty;
    public required string GuildId { get; init; } = string.Empty;
    public required string ChannelId { get; init; } = string.Empty;
}
