using DiscordBots.OpenAI.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DiscordBots.OpenAI;

public static class ServiceCollectionExtensions
{
    public static void AddOpenAi(this IServiceCollection services, IConfiguration config)
    {
        var apiKey =
            Get("ApiKey", "OPENAI_API_KEY")
            ?? throw new InvalidOperationException("OPENAI_API_KEY / OpenAI:ApiKey is required");

        var model = Get("Model", "OPENAI_MODEL") ?? "gpt-3.5-turbo";
        var baseUrl = (Get("BaseUrl", "OPENAI_BASE_URL") ?? "https://api.openai.com").Trim();
        if (baseUrl.EndsWith($"/"))
            baseUrl = baseUrl.TrimEnd('/');

        var org = Get("Organization", "OPENAI_ORG");
        var project = Get("Project", "OPENAI_PROJECT");

        services.Configure<OpenAiOptions>(options =>
        {
            options.ApiKey = apiKey;
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
            org,
            project
        ));
        return;

        string? Get(string keySuffix, string envVar)
        {
            var value = config[$"{OpenAiOptions.SectionName}:{keySuffix}"];
            return string.IsNullOrWhiteSpace(value)
                ? Environment.GetEnvironmentVariable(envVar)
                : value;
        }
    }
}

internal sealed class OpenAiConfigLogger(
    ILogger<OpenAiConfigLogger> logger,
    string baseUrl,
    string model,
    string? org,
    string? project
) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "OpenAI configured baseUrl={BaseUrl} model={Model} org={Org} project={Project}",
            baseUrl,
            model,
            org ?? "<none>",
            project ?? "<none>"
        );
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
