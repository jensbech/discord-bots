namespace DiscordBots.BookStack
{
    public sealed class BookStackOptions
    {
        public const string SectionName = "BookStack";
        public string BaseUrl { get; set; } = string.Empty;
        public string ApiId { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;

        // Optional: target guild id for posting webhook messages
        public string? GuildId { get; set; }

        // Optional: explicit channel id to post webhook messages to
        public string? ChannelId { get; set; }
    }
}
