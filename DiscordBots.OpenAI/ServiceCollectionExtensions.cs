using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DiscordBots.OpenAI;

public static class ServiceCollectionExtensions
{
    public static void AddOpenAi(this IServiceCollection services,
        IConfiguration config)
    {
        string? Get(string keySuffix, string envVar)
        {
            var value = config[$"{OpenAiOptions.SectionName}:{keySuffix}"];
            return string.IsNullOrWhiteSpace(value)
                ? Environment.GetEnvironmentVariable(envVar)
                : value;
        }

        var apiKey =
            Get("ApiKey", "OPENAI_API_KEY")
            ?? throw new InvalidOperationException("OPENAI_API_KEY / OpenAI:ApiKey is required");

        var model = Get("Model", "OPENAI_MODEL") ?? "gpt-3.5-turbo";
        var baseUrl = (Get("BaseUrl", "OPENAI_BASE_URL") ?? "https://api.openai.com").Trim();
        if (baseUrl.EndsWith("/"))
            baseUrl = baseUrl.TrimEnd('/');

        var maxTokens = int.TryParse(Get("MaxTokens", "OPENAI_MAX_TOKENS"), out var mt) ? mt : 1000;
        var org = Get("Organization", "OPENAI_ORG");
        var project = Get("Project", "OPENAI_PROJECT");

        services.Configure<OpenAiOptions>(options =>
        {
            options.ApiKey = apiKey;
            options.Model = model;
            options.MaxTokens = maxTokens;
            options.Project = project;
        });

        services.AddHttpClient<IOpenAiClient, OpenAiClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("User-Agent", "DiscordBots/OpenAIClient");
        });

        services.AddSingleton<IHostedService>(sp => new OpenAiConfigLogger(
            sp.GetRequiredService<ILogger<OpenAiConfigLogger>>(),
            baseUrl,
            model,
            apiKey,
            org,
            project
        ));
    }
}

internal sealed class OpenAiConfigLogger : IHostedService
{
    private readonly ILogger<OpenAiConfigLogger> _logger;
    private readonly string _baseUrl;
    private readonly string _model;
    private readonly string _apiKey;
    private readonly string? _org;
    private readonly string? _project;

    public OpenAiConfigLogger(
        ILogger<OpenAiConfigLogger> logger,
        string baseUrl,
        string model,
        string apiKey,
        string? org,
        string? project
    )
    {
        _logger = logger;
        _baseUrl = baseUrl;
        _model = model;
        _apiKey = apiKey;
        _org = org;
        _project = project;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "OpenAI configured baseUrl={BaseUrl} model={Model} keyPrefix={KeyPrefix} org={Org} project={Project}",
            _baseUrl,
            _model,
            _apiKey.Length >= 7 ? _apiKey[..7] : _apiKey,
            _org ?? "<none>",
            _project ?? "<none>"
        );
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
