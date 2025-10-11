using DiscordBots.BookStack.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DiscordBots.BookStack
{
    public static class ServiceCollectionExtensions
    {
        public static void AddBookStackService(this IServiceCollection services,
            IConfiguration builderConfiguration)
        {
            var baseUrl =
                Ensure("BaseUrl", "BOOKSTACK_BASE_URL", builderConfiguration);
            var apiId =
                Ensure("ApiId", "BOOKSTACK_API_ID", builderConfiguration);
            var apiKey =
                Ensure("ApiKey", "BOOKSTACK_API_KEY", builderConfiguration);
            var guildId =
                Ensure("GuildId", "BOOKSTACK_GUILD_ID", builderConfiguration);
            var channelId =
                Ensure("ChannelId", "BOOKSTACK_CHANNEL_ID", builderConfiguration);

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
                    new HttpClientHandler()
                )
                .ConfigureHttpClient(client =>
                {
                    client.DefaultRequestVersion = new Version(1, 1);
                    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
                });

           
        }
        private static string Ensure(string keySuffix, string envVar, IConfiguration appBuilderConfiguration)
        {
            var value = appBuilderConfiguration[$"{BookStackOptions.SectionName}:{keySuffix}"];
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            var envValue = Environment.GetEnvironmentVariable(envVar);
            return string.IsNullOrWhiteSpace(envValue) ? throw new InvalidOperationException($"Environment variable '{envVar}' is not set or is empty.") : envValue;
        }
    }
    
}
