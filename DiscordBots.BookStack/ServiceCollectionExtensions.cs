using DiscordBots.BookStack.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DiscordBots.BookStack
{
    public static class ServiceCollectionExtensions
    {
        public static void AddBookStackService(this IServiceCollection services,
            IConfiguration appBuilderConfiguration)
        {
            var baseUrl =
                GetEnvironmentValue("BaseUrl", "BOOKSTACK_BASE_URL")
                ?? throw new InvalidOperationException(
                    "BOOKSTACK_BASE_URL / BookStack:BaseUrl is required"
                );
            
            var apiId =
                GetEnvironmentValue("ApiId", "BOOKSTACK_API_ID")
                ?? throw new InvalidOperationException(
                    "BOOKSTACK_API_ID / BookStack:ApiId is required"
                );
            
            var apiKey =
                GetEnvironmentValue("ApiKey", "BOOKSTACK_API_KEY")
                ?? throw new InvalidOperationException(
                    "BOOKSTACK_API_KEY / BookStack:ApiKey is required"
                );
            
            var guildId =
                GetEnvironmentValue("GuildId", "BOOKSTACK_GUILD_ID")
                ?? throw new InvalidOperationException("BOOKSTACK_GUILD_ID is required");
            var channelId =
                GetEnvironmentValue("ChannelId", "BOOKSTACK_CHANNEL_ID")
                ?? throw new InvalidOperationException("BOOKSTACK_CHANNEL_ID is required");

            var trimmed = baseUrl.TrimEnd('/');
            
            if (!trimmed.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
            {
                trimmed += "/api";
            }
            var normalizedBaseUrl = trimmed + "/";

            services.Configure<BookStackOptions>(o =>
            {
                o.ApiId = apiId;
                o.ApiKey = apiKey;
                o.GuildId = guildId;
                o.ChannelId = channelId;
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
                    new HttpClientHandler() { MaxConnectionsPerServer = 1, UseCookies = false }
                )
                .ConfigureHttpClient(client =>
                {
                    client.DefaultRequestVersion = new Version(1, 1);
                    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
                });
            return;

            string? GetEnvironmentValue(string keySuffix, string envVar)
            {
                var value = appBuilderConfiguration[$"{BookStackOptions.SectionName}:{keySuffix}"];
                return string.IsNullOrWhiteSpace(value)
                    ? Environment.GetEnvironmentVariable(envVar)
                    : value;
            }
        }
    }
}
