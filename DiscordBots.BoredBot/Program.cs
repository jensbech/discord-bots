using DiscordBots.BookStack;
using DiscordBots.Core;
using DiscordBots.Core.Webhooks;
using DiscordBots.OpenAI;
using Microsoft.AspNetCore.Builder;
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
        builder.Services.AddWebhookProcessing(builder.Configuration);
        builder.Services.AddSingleton<IWebhookHandler, TestWebhookHandler>();

        builder.AddDiscordBot<BoredBot>(
            BoredBot.GetOrCreateInstance,
            CommandBuilders.Commands,
            "Bored Bot"
        );

        var app = builder.Build();
        app.MapWebhookEndpoints();

        await app.RunAsync();
    }
}
