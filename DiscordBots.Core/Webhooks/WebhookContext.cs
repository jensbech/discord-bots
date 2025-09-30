using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace DiscordBots.Core.Webhooks;

public sealed class WebhookContext(string source, JsonElement payload, ILogger logger)
{
    public string Source { get; } = source;
    public JsonElement Payload { get; } = payload;
    public ILogger Logger { get; } = logger;
    public IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();

    public void Set(object key, object? value) => Items[key] = value;

    public T? Get<T>(object key) => Items.TryGetValue(key, out var v) && v is T t ? t : default;
}
