using System.Text.Json.Serialization;

namespace DiscordBots.BookStack.Models
{
    public sealed class BookStackSearchResponse
    {
        public int Total { get; init; }

        [JsonPropertyName("data")]
        public required IReadOnlyList<BookStackSearchResult> Data { get; init; }

        public sealed class BookStackSearchResult
        {
            [JsonPropertyName("name")]
            public required string Name { get; init; }

            [JsonPropertyName("url")]
            public required string Url { get; init; }

            [JsonPropertyName("preview_html")]
            public required PreviewHtml PreviewHtml { get; init; }
        }

        public sealed class PreviewHtml
        {
            [JsonPropertyName("content")]
            public required string Content { get; init; }
        }
    }
}
