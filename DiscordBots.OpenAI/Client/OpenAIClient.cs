using System.Net.Http.Headers;
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

    public async Task<string?> ChatAboutDndRulesAsync(
        string question,
        CancellationToken ct = default
    )
    {
        var systemPrompt =
            "You are a chatbot replying ONLY to questions about Dungeons and Dragons 5E rules. "
            + "You refuse to discuss anything else but DND rules.";

        var request = new ChatCompletionRequest
        {
            Model = _options.Model,
            Messages =
            [
                new ChatMessage { Role = "system", Content = systemPrompt },
                new ChatMessage { Role = "user", Content = question },
            ],
            MaxTokens = _options.MaxTokens,
            Temperature = 0.7,
        };

        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                _options.ApiKey
            );
            if (!string.IsNullOrWhiteSpace(_options.Organization))
            {
                httpRequest.Headers.Add("OpenAI-Organization", _options.Organization);
            }
            if (!string.IsNullOrWhiteSpace(_options.Project))
            {
                httpRequest.Headers.Add("OpenAI-Project", _options.Project);
            }
            _logger.LogDebug(
                "Sending OpenAI chat request model={Model} keyPrefix={Prefix} headers=[{Headers}]",
                _options.Model,
                _options.ApiKey.Length >= 7 ? _options.ApiKey[..7] : _options.ApiKey,
                string.Join(
                    ";",
                    httpRequest.Headers.Select(h =>
                        h.Key
                        + (
                            h.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase)
                                ? "(set)"
                                : ""
                        )
                    )
                )
            );
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
                string? body = null;
                try
                {
                    body = await response.Content.ReadAsStringAsync(ct);
                }
                catch { }
                _logger.LogWarning(
                    "OpenAI API call failed with {Status} body snippet: {Body}",
                    response.StatusCode,
                    body is null ? "<empty>" : body[..Math.Min(200, body.Length)]
                );
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

    public async Task<string?> ChatWithContextAsync(
        string question,
        IReadOnlyList<string> documents,
        CancellationToken ct = default
    )
    {
        var limitedDocs = documents
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Select(d => d.Length > 3000 ? d[..3000] + "…" : d)
            .Take(8)
            .ToList();

        var contextBlocks = string.Join(
            "\n\n---\n\n",
            limitedDocs.Select((d, i) => $"Document {i + 1}:\n{d}")
        );

        var systemPrompt =
            "Write a comprehensive, well-structured answer (multiple paragraphs) summarizing and synthesizing the information. Write ALL that is required, without restraint"
            + "Assume the reader is familiar with the setting, no need for fluff about that."
            + "If any retrieved article does not relate to the question, omit it from your answer."
            + "Your tone is that of a story teller, but your job is to reproduce the source material in a factual way. You may assume the reader is already familiar with the world setting";

        var request = new ChatCompletionRequest
        {
            Model = _options.Model,
            Messages =
            [
                new ChatMessage
                {
                    Role = "system",
                    Content =
                        "You are a retrieval-augmented lore assistant. Read the user question, and then the retrieved articles from the. question.",
                },
                new ChatMessage
                {
                    Role = "user",
                    Content =
                        $"User Question: {question}\n\nInstructions: {systemPrompt}. Context for answerving your query: {contextBlocks}",
                },
            ],
            MaxTokens = _options.MaxTokens,
            Temperature = 0.4,
        };

        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                _options.ApiKey
            );
            httpRequest.Headers.Add("OpenAI-Organization", _options.Organization);
            httpRequest.Headers.Add("OpenAI-Project", _options.Project);

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
                string? body = null;
                try
                {
                    body = await response.Content.ReadAsStringAsync(ct);
                }
                catch { }
                _logger.LogWarning(
                    "OpenAI contextual API call failed with {Status} body snippet: {Body}",
                    response.StatusCode,
                    body is null ? "<empty>" : body[..Math.Min(200, body.Length)]
                );
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
            _logger.LogError(ex, "Error performing OpenAI contextual chat completion");
            return null;
        }
    }
}
