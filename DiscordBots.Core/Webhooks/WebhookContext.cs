using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace DiscordBots.Core.Webhooks;

public sealed class WebhookContext
{
    public string Source { get; }
    public JsonElement Payload { get; }
    public ILogger Logger { get; }
    public IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();

    public WebhookContext(string source, JsonElement payload, ILogger logger)
    {
        Source = source;
        Payload = payload;
        Logger = logger;
    }

    public void Set(object key, object? value) => Items[key] = value;

    public T? Get<T>(object key) => Items.TryGetValue(key, out var v) && v is T t ? t : default;
}
