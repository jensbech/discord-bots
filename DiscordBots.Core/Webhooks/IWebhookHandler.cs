namespace DiscordBots.Core.Webhooks;

public interface IWebhookHandler
{
    string Name { get; }
    bool CanHandle(string source);
    Task<WebhookHandledResult> Handle(WebhookContext context, CancellationToken ct = default);
}

public sealed record WebhookHandledResult(bool Handled, string? Reason = null)
{
    public static WebhookHandledResult Ok() => new(true);

    public static WebhookHandledResult NotHandled(string? reason = null) => new(false, reason);
}
