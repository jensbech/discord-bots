namespace DiscordBots.BookStack.Models
{
    public sealed class BookStackSearchResponse
    {
        public int Total { get; init; }
        public required IReadOnlyList<BookStackSearchResult> Data { get; init; }
    }

    public sealed class BookStackSearchResult
    {
        public required string Name { get; init; }
        public required string Url { get; init; }
        public required PreviewHtml Preview_Html { get; init; }
    }

    public sealed class PreviewHtml
    {
        public required string Content { get; init; }
    }
}
