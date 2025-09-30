using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
#if NET8_0_OR_GREATER
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
#endif

namespace DiscordBots.Core.Webhooks;

public static class WebhookHostingExtensions
{
    public static IHostApplicationBuilder AddWebhookProcessing(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<WebhookOptions>(
            builder.Configuration.GetSection(WebhookOptions.SectionName)
        );
        builder.Services.AddSingleton<IWebhookDispatcher, WebhookDispatcher>();
        return builder;
    }

#if NET8_0_OR_GREATER
    public static void MapWebhookEndpoints(this WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Webhooks");
        var options = app.Services.GetRequiredService<IOptions<WebhookOptions>>().Value;
        var dispatcher = app.Services.GetRequiredService<IWebhookDispatcher>();

        app.MapPost(
            "/webhooks/{source}",
            async (
                HttpRequest request,
                HttpResponse response,
                string source,
                CancellationToken ct
            ) =>
            {
                if (
                    options.AllowedSources.Length > 0
                    && !options.AllowedSources.Contains(source, StringComparer.OrdinalIgnoreCase)
                )
                {
                    response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }
                try
                {
                    using var doc = await JsonDocument.ParseAsync(
                        request.Body,
                        cancellationToken: ct
                    );
                    var result = await dispatcher.DispatchAsync(source, doc.RootElement, ct);
                    response.StatusCode = result.Handled
                        ? StatusCodes.Status200OK
                        : StatusCodes.Status202Accepted;
                }
                catch (JsonException)
                {
                    response.StatusCode = StatusCodes.Status400BadRequest;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unhandled webhook processing error");
                    response.StatusCode = StatusCodes.Status500InternalServerError;
                }
            }
        );

        app.MapPost(
            "/webhooks/new_post",
            async (HttpRequest request, HttpResponse response, CancellationToken ct) =>
            {
                try
                {
                    using var doc = await JsonDocument.ParseAsync(
                        request.Body,
                        cancellationToken: ct
                    );
                    var result = await dispatcher.DispatchAsync("bookstack", doc.RootElement, ct);
                    response.StatusCode = result.Handled
                        ? StatusCodes.Status200OK
                        : StatusCodes.Status202Accepted;
                }
                catch (JsonException)
                {
                    response.StatusCode = StatusCodes.Status400BadRequest;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unhandled webhook processing error");
                    response.StatusCode = StatusCodes.Status500InternalServerError;
                }
            }
        );
    }
#endif
}
