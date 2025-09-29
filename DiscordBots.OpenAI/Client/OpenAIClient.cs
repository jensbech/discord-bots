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
        if (string.IsNullOrWhiteSpace(question))
            return null;
        var limitedDocs = documents
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Select(d => d.Length > 3000 ? d[..3000] + "…" : d)
            .Take(8) // keep token usage sane
            .ToList();

        var contextBlocks = string.Join(
            "\n\n---\n\n",
            limitedDocs.Select((d, i) => $"Document {i + 1}:\n{d}")
        );

        var systemPrompt =
            "You are a retrieval-augmented lore assistant. Read the user question, then rely ONLY on the provided documents. "
            + "Assume the reader is familiar with the setting, no need for fluff about that."
            + "Write a comprehensive, well-structured answer (multiple paragraphs) summarising and synthesising the information. Write ALL that is required, without restraint"
            + "Do NOT include a Sources section, do NOT list document numbers, and do NOT output raw URLs or markdown links. "
            + "Never fabricate details that are not clearly implied by the documents. If partial info exists, state what is known and what is unknown. "
            + "Do not refer to the source material as source material or documents. Discuss it as if it is a truth or just facts. "
            + "Your tone is that of a story teller, but your job is to reproduce the source material in a factual way. You may assume the reader is already familiar with the world setting";

        var request = new ChatCompletionRequest
        {
            Model = _options.Model,
            Messages = new[]
            {
                new ChatMessage { Role = "system", Content = systemPrompt },
                new ChatMessage
                {
                    Role = "user",
                    Content =
                        $"User Question: {question}\n\nInstructions: Use the documents below to answer. If unsure, state that the documents do not contain the answer. Do not use outside knowledge.\n\nDocuments:\n{contextBlocks}",
                },
            },
            MaxTokens = Math.Min(_options.MaxTokens, 1200),
            Temperature = 0.25,
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
                "Sending OpenAI contextual chat request model={Model} keyPrefix={Prefix} headers=[{Headers}] docs={DocCount}",
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
                ),
                documents.Count
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
