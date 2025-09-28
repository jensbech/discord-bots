namespace DiscordBots.OpenAI.Models
{
    public sealed class ChatCompletionRequest
    {
        public required string Model { get; init; }
        public required IReadOnlyList<ChatMessage> Messages { get; init; }
        public int? MaxTokens { get; init; }
        public double? Temperature { get; init; }
    }

    public sealed class ChatMessage
    {
        public required string Role { get; init; }
        public required string Content { get; init; }
    }

    public sealed class ChatCompletionResponse
    {
        public required IReadOnlyList<ChatChoice> Choices { get; init; }
    }

    public sealed class ChatChoice
    {
        public required ChatMessage Message { get; init; }
    }
}
