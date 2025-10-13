using System.Net;
using DiscordBots.BookStack.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DiscordBots.BookStack;

public static class BookStackService
{
    public static void AddService(this IServiceCollection services, IConfiguration builderConfiguration)
    {
        var baseUrl = EnsureEnvironmentVariable(
            "BaseUrl",
            "BOOKSTACK_BASE_URL",
            builderConfiguration
        );
        var apiId = EnsureEnvironmentVariable("ApiId", "BOOKSTACK_API_ID", builderConfiguration);
        var apiKey = EnsureEnvironmentVariable("ApiKey", "BOOKSTACK_API_KEY", builderConfiguration);
        // var guildId = EnsureEnvironmentVariable("GuildId", "BOOKSTACK_GUILD_ID", builderConfiguration);
        // var channelId = EnsureEnvironmentVariable("ChannelId", "BOOKSTACK_CHANNEL_ID", builderConfiguration);

        var baseUrlTrimmed = baseUrl.TrimEnd('/');

        if (!baseUrlTrimmed.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
        {
            baseUrlTrimmed += "/api";
        }
        var normalizedBaseUrl = baseUrlTrimmed + "/";

        services.Configure<BookStackOptions>(o =>
        {
            o.ApiId = apiId;
            o.ApiKey = apiKey;
            // o.GuildId = guildId;
            // o.ChannelId = channelId;
        });

        services
            .AddHttpClient<IBookStackClient, BookStackClient>(client =>
            {
                client.BaseAddress = new Uri(normalizedBaseUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("User-Agent", "DiscordBots/BookStackClient");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
                new SocketsHttpHandler
                {
                    AutomaticDecompression =
                        DecompressionMethods.GZip | DecompressionMethods.Deflate,
                    UseCookies = false,
                    AllowAutoRedirect = false
                }
            )
            .ConfigureHttpClient(client =>
            {
                client.DefaultRequestVersion = new Version(1, 1);
                client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
            });
    }

    private static string EnsureEnvironmentVariable(
        string keySuffix,
        string envVar,
        IConfiguration appBuilderConfiguration
    )
    {
        var value = appBuilderConfiguration[$"{BookStackOptions.SectionName}:{keySuffix}"];
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var envValue = Environment.GetEnvironmentVariable(envVar);
        return string.IsNullOrWhiteSpace(envValue)
            ? throw new InvalidOperationException(
                $"Environment variable '{envVar}' is not set or is empty."
            )
            : envValue;
    }
}
