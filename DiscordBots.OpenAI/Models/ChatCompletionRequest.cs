namespace DiscordBots.OpenAI.Models;

using System.Text.Json.Serialization;

public sealed class ChatCompletionResponse
{
    [JsonPropertyName("choices")]
    public required IReadOnlyList<ChatChoice> Choices { get; init; }
}

public sealed class ChatChoice(ChatMessage message)
{
    [JsonPropertyName("message")]
    public required ChatMessage Message { get; init; } = message;
}
