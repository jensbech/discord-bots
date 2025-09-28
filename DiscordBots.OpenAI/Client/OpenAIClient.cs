using System.Net.Http.Json;
using System.Text.Json;
using DiscordBots.OpenAI.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiscordBots.OpenAI;

internal sealed class OpenAIClient(
    HttpClient http,
    IOptions<OpenAIOptions> options,
    ILogger<OpenAIClient> logger
) : IOpenAIClient
{
    private readonly HttpClient _http = http;
    private readonly OpenAIOptions _options = options.Value;
    private readonly ILogger<OpenAIClient> _logger = logger;

    public async Task<string?> ChatAsync(string question, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(question))
            return null;

        var systemPrompt =
            "You are a chatbot replying ONLY to questions about Dungeons and Dragons 5E rules. "
            + "You refuse to discuss anything else but DND rules.";

        var request = new ChatCompletionRequest
        {
            Model = _options.Model,
            Messages = new[]
            {
                new ChatMessage { Role = "system", Content = systemPrompt },
                new ChatMessage { Role = "user", Content = question },
            },
            MaxTokens = _options.MaxTokens,
            Temperature = 0.7,
        };

        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions");
            httpRequest.Headers.Add("Authorization", $"Bearer {_options.ApiKey}");
            httpRequest.Content = JsonContent.Create(
                request,
                options: new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                }
            );

            var response = await _http.SendAsync(httpRequest, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OpenAI API call failed with {Status}", response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                },
                ct
            );

            return result?.Choices.FirstOrDefault()?.Message.Content;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing OpenAI chat completion");
            return null;
        }
    }
}
