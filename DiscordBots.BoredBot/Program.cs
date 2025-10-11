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
        var builder = WebApplication.CreateBuilder();
        
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Debug);

        builder.Services.AddBookStackService(builder.Configuration);
        builder.Services.AddOpenAi(builder.Configuration);
        builder.Services.AddSingleton(CommandBuilders.Commands);
        builder.Services.AddSingleton("Bored Bot");
        builder.Services.AddHostedService<DiscordBotService<BoredBot>>();
        
        var app = builder.Build();

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
