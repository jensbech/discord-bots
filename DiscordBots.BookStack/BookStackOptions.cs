namespace DiscordBots.BookStack
{
    public sealed class BookStackOptions
    {
        public const string SectionName = "BookStack";
        public required string BaseUrl { get; set; } = string.Empty;
        public required string ApiId { get; set; } = string.Empty;
        public required string ApiKey { get; set; } = string.Empty;
        public required string GuildId { get; set; } = string.Empty;
        public required string ChannelId { get; set; } = string.Empty;
    }
}
