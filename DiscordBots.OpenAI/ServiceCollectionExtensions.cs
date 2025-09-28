using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DiscordBots.OpenAI;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenAI(
        this IServiceCollection services,
        IConfiguration config
    )
    {
        string? Get(string keySuffix, string envVar)
        {
            var value = config[$"{OpenAIOptions.SectionName}:{keySuffix}"];
            return string.IsNullOrWhiteSpace(value)
                ? Environment.GetEnvironmentVariable(envVar)
                : value;
        }

        var apiKey =
            Get("ApiKey", "OPENAI_API_KEY")
            ?? throw new InvalidOperationException("OPENAI_API_KEY / OpenAI:ApiKey is required");

        var model = Get("Model", "OPENAI_MODEL") ?? "gpt-3.5-turbo";
        var baseUrl = Get("BaseUrl", "OPENAI_BASE_URL") ?? "https://api.openai.com";
        var maxTokens = int.TryParse(Get("MaxTokens", "OPENAI_MAX_TOKENS"), out var mt) ? mt : 1000;

        services.AddSingleton(
            new OpenAIOptions
            {
                ApiKey = apiKey,
                Model = model,
                BaseUrl = baseUrl,
                MaxTokens = maxTokens,
            }
        );

        services.AddHttpClient<IOpenAIClient, OpenAIClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("User-Agent", "DiscordBots/OpenAIClient");
        });

        return services;
    }
}
