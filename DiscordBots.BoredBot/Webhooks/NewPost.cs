using System.Text.Json;
using Discord;
using DiscordBots.BookStack;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DiscordBots.BoredBot.Webhooks;

public class NewPost
{
    public static async Task<IResult> SendAsync(HttpRequest request, IServiceProvider sp)
    {
        var loggerFactory = LoggerFactory.Create(builder => { });
        var logger = loggerFactory.CreateLogger("NewPost");
        try
        {
            var body = await JsonDocument.ParseAsync(request.Body);
            var url = body.RootElement.GetProperty("url").GetString();

            var author = body
                .RootElement.GetProperty("triggered_by")
                .GetProperty("name")
                .GetString();

            var title = body
                .RootElement.GetProperty("current_revision")
                .GetProperty("name")
                .GetString();

            var embed = new EmbedBuilder
            {
                Title = title,
                Author = new EmbedAuthorBuilder { Name = author },
                Description = "A new post has appeared in the Wiki!",
                Url = url,
            }.Build();

            var bookStackOpts =
                sp.GetService<Microsoft.Extensions.Options.IOptions<BookStackOptions>>()?.Value;

            var client = BoredBot.Instance?.GetClient();

            if (client is null)
            {
                return Results.StatusCode(503);
            }

            ITextChannel? target = null;

            var channelIdStr =
                bookStackOpts?.ChannelId ?? throw new Exception("Channel Id not set");

            if (!ulong.TryParse(channelIdStr, out var channelId))
            {
                throw new Exception("Channel Id is not a valid ulong");
            }

            target =
                client.GetChannel(channelId) as ITextChannel
                ?? throw new Exception("Could not determine channel");

            await target.SendMessageAsync(text: null, embed: embed);
            return Results.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled error processing newpost webhook");
            return Results.StatusCode(500);
        }
    }
}
