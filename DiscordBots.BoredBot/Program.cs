using DiscordBots.BookStack;
using DiscordBots.BoredBot.Webhooks;
using DiscordBots.Core;
using DiscordBots.OpenAI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiscordBots.BoredBot;

public static class Program
{
    public static async Task Main()
    {
        var appBuilder = WebApplication.CreateBuilder();
        appBuilder.Logging.AddConsole();
        appBuilder.Logging.SetMinimumLevel(LogLevel.Debug);

        appBuilder.Services.AddBookStackService(appBuilder.Configuration);
        appBuilder.Services.AddOpenAI(appBuilder.Configuration);
        appBuilder.Services.AddHostedService<ServiceInitializer>();

        appBuilder.AddDiscordBot<BoredBot>(
            BoredBot.GetOrCreateInstance,
            CommandBuilders.Commands,
            "Bored Bot"
        );

        var app = appBuilder.Build();

        app.MapPost(
            "/webhooks/new_post",
            (
                HttpRequest request,
                ILogger<NewPost> logger,
                IOptions<BookStackOptions> bookstackOptions
            ) =>
            {
                _ = NewPost.SendAsync(request, logger, bookstackOptions);
            }
        );

        await app.RunAsync();
    }
}
