namespace DiscordBots.BookStack.Models
{
    public sealed class BookStackSearchResponse
    {
        public int Total { get; init; }
        public required IReadOnlyList<BookStackSearchResult> data { get; init; }
    }

    public sealed class BookStackSearchResult
    {
        public required string name { get; init; }
        public required string url { get; init; }
        public required PreviewHtml preview_html { get; init; }
    }

    public sealed class PreviewHtml
    {
        public required string content { get; init; }
    }
}
