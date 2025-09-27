using Discord;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DiscordBots.Core;

public static class DiscordBotHostingExtensions
{
    public static IHostApplicationBuilder AddDiscordBot<TBot>(
        this IHostApplicationBuilder builder,
        Func<BotEnvironmentVariables, SlashCommandBuilder[], ILogger<TBot>, Task<TBot>> botFactory,
        SlashCommandBuilder[] commands,
        string botName
    )
        where TBot : DiscordBot
    {
        builder.Services.AddSingleton(botFactory);
        builder.Services.AddSingleton(commands);
        builder.Services.AddSingleton(botName);
        builder.Services.AddHostedService<DiscordBotService<TBot>>();

        return builder;
    }

    public static IHost CreateDiscordBotHost<TBot>(
        string[] args,
        Func<BotEnvironmentVariables, SlashCommandBuilder[], ILogger<TBot>, Task<TBot>> botFactory,
        SlashCommandBuilder[] commands,
        string botName
    )
        where TBot : DiscordBot
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Logging.AddConsole();
        builder.AddDiscordBot(botFactory, commands, botName);
        return builder.Build();
    }
}
