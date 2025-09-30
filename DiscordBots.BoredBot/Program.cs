using System.Text.Json;
using DiscordBots.BookStack;
using DiscordBots.Core;
using DiscordBots.OpenAI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DiscordBots.BoredBot;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Debug);

        builder.Services.AddBookStack(builder.Configuration);
        builder.Services.AddOpenAI(builder.Configuration);
        builder.Services.AddHostedService<ServiceInitializer>();
        // Webhooks previously used a dispatcher + handlers. Simplified to a single endpoint below.

        builder.AddDiscordBot<BoredBot>(
            BoredBot.GetOrCreateInstance,
            CommandBuilders.Commands,
            "Bored Bot"
        );

        var app = builder.Build();

        // Minimal webhook endpoint: POST /webhooks/newpost
        // Body: arbitrary JSON forwarded to discord channel (TODO: implement actual posting logic)
        app.MapPost(
            "/webhooks/newpost",
            async (HttpRequest request, ILoggerFactory lf) =>
            {
                var logger = lf.CreateLogger("NewPostWebhook");
                try
                {
                    using var doc = await JsonDocument.ParseAsync(request.Body);
                    logger.LogInformation(
                        "Received newpost webhook: {Json}",
                        doc.RootElement.ToString()
                    );
                    // TODO: Add logic to send a message to a channel if needed.
                    return Results.Ok(new { status = "ok" });
                }
                catch (JsonException)
                {
                    return Results.BadRequest(new { error = "invalid_json" });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unhandled error processing newpost webhook");
                    return Results.StatusCode(500);
                }
            }
        );

        await app.RunAsync();
    }
}
