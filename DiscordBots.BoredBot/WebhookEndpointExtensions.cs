using System.Text.Json;
using DiscordBots.Core.Webhooks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DiscordBots.BoredBot;

internal static class WebhookEndpointExtensions
{
    public static void MapWebhookEndpoints(this WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Webhooks");
        var options = app
            .Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<WebhookOptions>>()
            .Value;
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
}
