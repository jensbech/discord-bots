using System.Net.Http.Headers;
using System.Net.Http.Json;
using DiscordBots.OpenAI.Interfaces;
using DiscordBots.OpenAI.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DiscordBots.OpenAI.Prompts;
namespace DiscordBots.OpenAI;

internal sealed class OpenAiClient(
    HttpClient http,
    IOptions<OpenAiOptions> options,
    ILogger<OpenAiClient> logger
) : IOpenAiClient
{
    public async Task<string?> RulesChat(string question)
    {
        var systemPrompt = Prompts.RulesChat.GetSystemPrompt();
        var result = await http.SendAsync(ConstructCompletionRequest(question, systemPrompt));
        var answer = await EnsureChatAnswerString(result);
        return answer;
    }

    public async Task<string?> AskChat(string query, IReadOnlyList<string> documents)
    {
        var (systemPrompt, question) = Prompts.AskChat.Get(query, [.. documents]);
        var res = await http.SendAsync(ConstructCompletionRequest(question, systemPrompt));
        return await EnsureChatAnswerString(res);
    }

    public async Task<string?> ReduceSearchQuery(string question)
    {
        var systemPrompt = SearchQueryReducer.GetSystemPrompt();
        var res = await http.SendAsync(ConstructCompletionRequest(question, systemPrompt, "gpt-3.5-turbo"));
        return await EnsureChatAnswerString(res);
    }

    private HttpRequestMessage ConstructCompletionRequest(string question, string? systemPrompt, string? modelOverride = null)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.ApiKey);
        req.Headers.Add("OpenAI-Project", options.Value.Project);

        List<ChatMessage> messages = [];

        if (!string.IsNullOrEmpty(systemPrompt))
        {
            messages.Add(new ChatMessage { Role = "system", Content = systemPrompt });
        }
        messages.Add(new ChatMessage { Role = "user", Content = question });
        
        var chatRequest = new 
        {
            Model = modelOverride ?? options.Value.Model,
            Messages = messages,
            Temperature = 0.2,
        };

        req.Content = JsonContent.Create(chatRequest);
        return req;
    }

    private async Task<string> EnsureChatAnswerString(HttpResponseMessage httpResponse)
    {
        if (!httpResponse.IsSuccessStatusCode)
        {
            var errorContent = await httpResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"Error response: {errorContent}");
            var error = $"Failed to get response from OpenAI: '{httpResponse.StatusCode}'";
            logger.LogError(error, httpResponse.StatusCode);
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
