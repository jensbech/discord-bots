using System.Text.Json;
using Discord;
using DiscordBots.BookStack;
using DiscordBots.Core;
using DiscordBots.OpenAI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
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
        builder.AddDiscordBot<BoredBot>(
            BoredBot.GetOrCreateInstance,
            CommandBuilders.Commands,
            "Bored Bot"
        );

        var app = builder.Build();

        _ = app.MapPost(
            "/webhooks/new_post",
            (HttpRequest request, IServiceProvider sp) =>
            {
                _ = Webhooks.NewPost.SendAsync(request, sp);
            }
        );

        await app.RunAsync();
    }
}
