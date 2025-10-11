using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace DiscordBots.Core.Logging;

public static class LoggerExtensions
{
    private static string BuildContext(SocketUser user, ISocketMessageChannel channel)
    {
        var guildName = (channel as SocketGuildChannel)?.Guild?.Name ?? "DM";
        var channelName =
            (channel as SocketGuildChannel)?.Name
            ?? (channel is SocketDMChannel ? "DM" : channel.Name);
        var userName = user.GlobalName ?? user.Username;
        return $"[User:{userName}({user.Id}) Guild:{guildName} Channel:{channelName}]";
    }

    public static void LogIncomingUserMessage(this ILogger logger, SocketMessage message)
    {
        if (message.Author.IsBot)
            return;
        logger.LogInformation(
            "{Context} {Content}",
            BuildContext(message.Author, message.Channel),
            message.Content
        );
    }

    private static void LogSlashCommandError(
        this ILogger logger,
        SocketUser user,
        ISocketMessageChannel channel,
        string command,
        string? input,
        string error
    )
    {
        logger.LogWarning(
            "{Context} /{Command} {Input} ERROR: {Error}",
            BuildContext(user, channel),
            command,
            input,
            error
        );
    }

    public static void LogSlashError(this ILogger logger, SocketSlashCommand cmd, string error)
    {
        var input = cmd.Data.Options?.FirstOrDefault()?.Value?.ToString();
        logger.LogSlashCommandError(cmd.User, cmd.Channel, cmd.CommandName, input, error);
    }
}
