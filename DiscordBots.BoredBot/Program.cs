using DiscordBots.BookStack;
using DiscordBots.BoredBot.Commands;
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
        appBuilder.Services.AddOpenAi(appBuilder.Configuration);
        
        appBuilder.AddDiscordBot<BoredBot>(
            CommandBuilders.Commands,
            "Bored Bot"
        );

        var app = appBuilder.Build();

        app.MapPost(
            "/webhooks/new_post",
            async (
                HttpRequest request,
                BoredBot bot,
                ILogger<NewPost> logger,
                IOptions<BookStackOptions> bookstackOptions
            ) => await NewPost.SendAsync(request, bot, logger, bookstackOptions)
        );

        await app.RunAsync();
    }
}
