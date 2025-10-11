using System.Text.Json.Serialization;

namespace DiscordBots.BookStack.Models
{
    public sealed class BookStackSearchResponse
    {
        public int Total { get; init; }

        [JsonPropertyName("data")]
        public required IReadOnlyList<BookStackSearchResult> Data { get; init; }

        public sealed class BookStackSearchResult(string url, PreviewHtml previewHtml, string name)
        {
            [JsonPropertyName("name")]
            public required string Name { get; init; } = name;

            [JsonPropertyName("url")]
            public required string Url { get; init; } = url;

            [JsonPropertyName("preview_html")]
            public required PreviewHtml PreviewHtml { get; init; } = previewHtml;
        }

        public sealed class PreviewHtml(string content)
        {
            [JsonPropertyName("content")]
            public required string Content { get; init; } = content;
        }
    }
}
