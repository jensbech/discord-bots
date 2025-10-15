using DiscordBots.BookStack;
using DiscordBots.Core;
using DiscordBots.OpenAI;
using DiscordBots.Scribe.Commands;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DiscordBots.Scribe;

public static class Program
{
    public static async Task Main()
    {
        var builder = WebApplication.CreateBuilder();

        var port = Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORT") ?? "9999";
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(int.Parse(port));
        });

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
        builder.Services.AddSingleton<ScribeBotBase>();
        builder.Services.AddHostedService<BotService<ScribeBotBase>>();

        var app = builder.Build();

        await app.RunAsync();
    }
}
