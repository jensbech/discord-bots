using DiscordBots.BookStack;
using DiscordBots.Core;
using DiscordBots.OpenAI;
using DiscordBots.Scribe.Commands;
using DiscordBots.Scribe.Webhooks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiscordBots.Scribe;

public static class Program
{
    public static async Task Main()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
                ? LogLevel.Debug
                : LogLevel.Information
        );

        BookStackService.AddService(builder.Services, builder.Configuration);
        OpenAiService.AddService(builder.Services, builder.Configuration);
        
        builder.Services.Configure<ScribeBotOptions>(options =>
        {
            options.BotName = "Scribe Bot";
            options.Token = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN") ?? string.Empty;
        });
        
        builder.Services.AddSingleton(CommandBuilders.Commands);
        builder.Services.AddHostedService<BotService<ScribeBotBase>>();

        var app = builder.Build();

        app.MapPost(
            "/webhooks/new_post",
            async (
                HttpRequest request,
                ScribeBotBase @base,
                ILogger<NewPost> logger,
                IOptions<BookStackOptions> bookstackOptions
            ) => await NewPost.SendAsync(request, @base, logger, bookstackOptions)
        );

        await app.RunAsync();
    }
}
