using System.Text.Json;
using Discord.WebSocket;
using DiscordBots.Core.Webhooks;
using Microsoft.Extensions.Logging;

namespace DiscordBots.BoredBot;

internal sealed class TestWebhookHandler : IWebhookHandler
{
    public string Name => "TestWebhookHandler";

    public bool CanHandle(string source) =>
        string.Equals(source, "test", StringComparison.OrdinalIgnoreCase);

    public static WebhookHandledResult Handle(
        WebhookContext context,
        CancellationToken ct = default
    )
    {
        try
        {
            var bot = BoredBot.Instance;
            var client = bot?.GetClient();

            var message = "Test webhook received";
            if (context.Payload.TryGetProperty("message", out var messageElement))
            {
                message = messageElement.GetString() ?? message;
            }

            context.Logger.LogInformation("Webhook received: {Message}", message);

            // var channelId = 1234567890123456789UL; // Replace with your actual channel ID
            // var channel = client.GetChannel(channelId) as ISocketMessageChannel;
            // if (channel != null)
            // {
            //     await channel.SendMessageAsync($"Webhook: {message}");
            // }

            return WebhookHandledResult.Ok();
        }
        catch (Exception ex)
        {
            context.Logger.LogError(ex, "Error handling test webhook");
            return WebhookHandledResult.NotHandled($"Error: {ex.Message}");
        }
    }

    Task<WebhookHandledResult> IWebhookHandler.Handle(WebhookContext context, CancellationToken ct)
    {
        var result = Handle(context, ct);
        return Task.FromResult(result);
    }
}
