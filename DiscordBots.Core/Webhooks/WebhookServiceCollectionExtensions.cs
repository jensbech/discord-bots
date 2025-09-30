using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DiscordBots.Core.Webhooks;

public static class WebhookServiceCollectionExtensions
{
    public static IServiceCollection AddWebhookProcessing(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<WebhookOptions>(configuration.GetSection(WebhookOptions.SectionName));
        services.AddSingleton<IWebhookDispatcher, WebhookDispatcher>();
        return services;
    }
}
