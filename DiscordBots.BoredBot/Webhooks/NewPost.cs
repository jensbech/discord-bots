using System.Text.Json;
using Discord;
using DiscordBots.BookStack;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiscordBots.BoredBot.Webhooks;

public class NewPost
{
    public static async Task<IResult> SendAsync(
        HttpRequest request,
        ILogger<NewPost> logger,
        IOptions<BookStackOptions> options
    )
    {
        try
        {
            var (url, author, title) = GetMessageContent(request);

            var embed = new EmbedBuilder()
                .WithTitle(title)
                .WithAuthor(author)
                .WithDescription("A new post has appeared in the Wiki!")
                .WithUrl(url)
                .Build();

            var guildId = ulong.Parse(options.Value.GuildId);
            var channelId = ulong.Parse(options.Value.ChannelId);

            var botClient = BoredBot.Instance.GetClient();
            var guild = botClient.GetGuild(guildId);
            var channel = guild.GetTextChannel(channelId);

            await channel.SendMessageAsync(text: null, embed: embed);
            return Results.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled error processing NewPost webhook");
            return Results.StatusCode(500);
        }
    }

    private static (string url, string author, string title) GetMessageContent(HttpRequest request)
    {
        var body = JsonDocument.Parse(request.Body);
        var root = body.RootElement;

        var url =
            root.GetProperty("url").GetString() ?? throw new InvalidOperationException(
                "url is null"
            );

        var author =
            root.GetProperty("triggered_by").GetProperty("name").GetString()
            ?? throw new InvalidOperationException("triggered_by.name is null");

        var title =
            root.GetProperty("current_revision").GetProperty("name").GetString()
            ?? throw new InvalidOperationException("current_revision.name is null");

        return (url, author, title);
    }
}
