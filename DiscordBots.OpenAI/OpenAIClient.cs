using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Discord;
using DiscordBots.OpenAI.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiscordBots.OpenAI;

public interface IOpenAiClient
{
    Task<string?> RulesChat(string question);
    Task<string?> AskChat(string question, IReadOnlyList<string> documents);
}

internal sealed class OpenAiClient(
    HttpClient http,
    IOptions<OpenAiOptions> options,
    ILogger<OpenAiClient> logger
) : IOpenAiClient
{
    private readonly HttpClient _http = http;
    private readonly OpenAiOptions _options = options.Value;
    private readonly ILogger<OpenAiClient> _logger = logger;

    public async Task<string?> RulesChat(string question)
    {
        var systemPrompt = OpenAI.RulesChat.Get();
        var res = await _http.SendAsync(ConstructCompletionRequest(question, systemPrompt));
        return await EnsureChatAnswerString(res);
    }

    public async Task<string?> AskChat(string query, IReadOnlyList<string> documents)
    {
        var (systemPrompt, question) = OpenAI.AskChat.Get(query, [.. documents]);
        var res = await _http.SendAsync(ConstructCompletionRequest(question, systemPrompt));
        return await EnsureChatAnswerString(res);
    }

    private HttpRequestMessage ConstructCompletionRequest(string question, string? systemPrompt)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        req.Headers.Add("OpenAI-Project", _options.Project);

        List<ChatMessage> messages = [];
        if (!string.IsNullOrEmpty(systemPrompt))
        {
            messages.Add(new ChatMessage { Role = "system", Content = systemPrompt });
        }
        messages.Add(new ChatMessage { Role = "user", Content = question });

        req.Content = JsonContent.Create(
            new ChatCompletionRequest
            {
                Model = _options.Model,
                Messages = messages,
                MaxTokens = _options.MaxTokens,
                Temperature = 0.3,
            }
        );
        return req;
    }

    private async Task<string> EnsureChatAnswerString(HttpResponseMessage httpResponse)
    {
        if (!httpResponse.IsSuccessStatusCode)
        {
            var errorContent = await httpResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"Error response: {errorContent}");
            var error = $"Failed to get response from OpenAI: '{httpResponse.StatusCode}'";
            _logger.LogError(error, httpResponse.StatusCode);
            throw new Exception(error);
        }

        var result =
            await httpResponse.Content.ReadFromJsonAsync<ChatCompletionResponse>()
            ?? throw new Exception("OpenAI response is null");

        return result.Choices.Count > 0
            ? result.Choices[0].Message.Content
            : throw new Exception("OpenAI response content is null");
    }
}
