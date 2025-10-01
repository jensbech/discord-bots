using System.Text.Json;
using Discord;
using DiscordBots.BookStack;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DiscordBots.BoredBot.Webhooks;

public class NewPost
{
    public static async Task<IResult> SendAsync(
        HttpRequest request,
        IServiceProvider serviceProvider
    )
    {
        var logger =
            serviceProvider.GetService<ILogger<NewPost>>()
            ?? LoggerFactory.Create(b => { }).CreateLogger<NewPost>();

        try
        {
            var (url, author, title) = await GetMessageContentAsync(request);

            var embed = new EmbedBuilder()
                .WithTitle(title)
                .WithAuthor(author)
                .WithDescription("A new post has appeared in the Wiki!")
                .WithUrl(url)
                .Build();

            var bookStackOpts =
                serviceProvider
                    .GetService<Microsoft.Extensions.Options.IOptions<BookStackOptions>>()
                    ?.Value
                ?? throw new InvalidOperationException("BookStack options not configured");

            if (!ulong.TryParse(bookStackOpts.ChannelId, out var channelId))
                throw new InvalidOperationException("Channel Id is not a valid ulong");

            var botClient =
                BoredBot.Instance?.GetClient()
                ?? throw new InvalidOperationException("Discord client is not available");

            var channelTarget =
                botClient.GetChannel(channelId) as ITextChannel
                ?? throw new InvalidOperationException("Could not determine target text channel");

            await channelTarget.SendMessageAsync(text: null, embed: embed);
            return Results.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled error processing NewPost webhook");
            return Results.StatusCode(500);
        }
    }

    private static async Task<(string url, string author, string title)> GetMessageContentAsync(
        HttpRequest request
    )
    {
        await using var stream = request.Body;
        using var doc = await JsonDocument.ParseAsync(stream);

        var root = doc.RootElement;

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
