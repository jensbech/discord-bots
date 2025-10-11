namespace DiscordBots.OpenAI.Models;

using System.Text.Json.Serialization;

public sealed class ChatCompletionRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
    [JsonPropertyName("messages")]
    public List<ChatMessage> Messages { get; set; } = new();
    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }
    [JsonPropertyName("max_tokens")]
    public int? MaxTokens { get; set; }
}

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