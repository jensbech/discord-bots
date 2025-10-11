using Discord;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DiscordBots.Core;

public static class DiscordBotHostingExtensions
{
    public static void AddDiscordBot<TBot>(this IHostApplicationBuilder builder,
        SlashCommandBuilder[] commands,
        string botName) where TBot : BaseDiscordBot
    {
        builder.Services.AddSingleton(commands);
        builder.Services.AddSingleton(botName);
        builder.Services.AddHostedService<DiscordBotService<TBot>>();
    }
}
