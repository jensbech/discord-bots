using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace DiscordBots.Core.Webhooks;

public interface IWebhookDispatcher
{
    Task<WebhookHandledResult> DispatchAsync(
        string source,
        JsonElement payload,
        CancellationToken ct = default
    );
}

internal sealed class WebhookDispatcher(
    IEnumerable<IWebhookHandler> handlers,
    ILogger<WebhookDispatcher> logger
) : IWebhookDispatcher
{
    private readonly IReadOnlyList<IWebhookHandler> _handlers = handlers.ToList();
    private readonly ILogger<WebhookDispatcher> _logger = logger;

    public async Task<WebhookHandledResult> DispatchAsync(
        string source,
        JsonElement payload,
        CancellationToken ct = default
    )
    {
        foreach (var handler in _handlers)
        {
            if (!handler.CanHandle(source))
                continue;
            try
            {
                var ctx = new WebhookContext(source, payload, _logger);
                var result = await handler.Handle(ctx, ct);
                if (result.Handled)
                    return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error handling webhook source {Source} handler {Handler}",
                    source,
                    handler.Name
                );
            }
        }
        return WebhookHandledResult.NotHandled();
    }
}
