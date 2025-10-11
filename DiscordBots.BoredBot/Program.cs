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
        builder.Logging.SetMinimumLevel(
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
                ? LogLevel.Debug
                : LogLevel.Information
        );

        builder.Services.Add(builder.Configuration);
        builder.Services.AddSingleton("Bored Bot");
        builder.Services.AddOpenAi(builder.Configuration);
        builder.Services.AddSingleton(CommandBuilders.Commands);
        builder.Services.AddHostedService<BotService<BoredBaseDiscordBot>>();

        var app = builder.Build();

        app.MapPost(
            "/webhooks/new_post",
            async (
                HttpRequest request,
                BoredBaseDiscordBot baseDiscordBot,
                ILogger<NewPost> logger,
                IOptions<BookStackOptions> bookstackOptions
            ) => await NewPost.SendAsync(request, baseDiscordBot, logger, bookstackOptions)
        );

        await app.RunAsync();
    }
}
