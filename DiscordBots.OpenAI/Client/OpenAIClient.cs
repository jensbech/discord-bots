using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Discord;
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

    public async Task<string?> ChatAsync(string question)
    {
        var systemPrompt =
            "You are a chatbot replying ONLY to questions about Dungeons and Dragons 5E rules. You refuse to discuss anything else but DND rules.";
        var response = await _http.SendAsync(ConstructCompletionRequest(question, systemPrompt));
        return await EnsureChatAnswerString(response);
    }

    public async Task<string?> ChatWithContextAsync(
        string question,
        IReadOnlyList<string> documents
    )
    {
        var systemPrompt =
            "Write a comprehensive, well-structured answer (multiple paragraphs) summarizing and synthesizing the information. Write ALL that is required, without restraint"
            + "Assume the reader is familiar with the setting, no need for fluff about that."
            + "If any retrieved article does not relate to the question, omit it from your answer."
            + "Your tone is that of a story teller, but your job is to reproduce the source material in a factual way. You may assume the reader is already familiar with the world setting";

        question =
            $"User Question: {question}\n\n Context for answering your query: {string.Join(
            "\n\n---\n\n",
            documents.Select((d, i) => $"Document {i + 1}:\n{d}")
        )}";

        var response = await _http.SendAsync(ConstructCompletionRequest(question, systemPrompt));
        return await EnsureChatAnswerString(response);
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
