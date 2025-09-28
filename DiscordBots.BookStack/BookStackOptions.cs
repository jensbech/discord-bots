namespace DiscordBots.BookStack
{
    public sealed class BookStackOptions
    {
        public const string SectionName = "BookStack";
        public string BaseUrl { get; set; } = string.Empty;
        public string ApiId { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string? GuildId { get; set; }
    }
}
