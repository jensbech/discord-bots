namespace DiscordBots.OpenAI.Models;

public sealed class ChatCompletionRequest;

public sealed class ChatMessage
{
    public required string Content { get; init; }
}

public sealed class ChatCompletionResponse
{
    public required IReadOnlyList<ChatChoice> Choices { get; init; }
}

public sealed class ChatChoice(ChatMessage message)
{
    public required ChatMessage Message { get; init; } = message;
}