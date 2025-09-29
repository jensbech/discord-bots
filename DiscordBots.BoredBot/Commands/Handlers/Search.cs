using Discord;
using Discord.WebSocket;
using DiscordBots.BookStack;
using Microsoft.Extensions.Logging;

namespace DiscordBots.BoredBot.Commands;

internal sealed class SearchHandler(IBookStackClient bookStackClient) : ISlashCommandHandler
{
    private readonly IBookStackClient _bookStackClient = bookStackClient;

    public string Name => "search";

    public async Task HandleAsync(SocketSlashCommand command, ILogger logger)
    {
        var query =
            command.Data.Options?.FirstOrDefault(o => o.Name == "query")?.Value?.ToString()
            ?? string.Empty;
        await command.DeferAsync();

        var result = await _bookStackClient.SearchAsync(query);

        if (result == null || result.data.Count == 0)
        {
            await command.FollowupAsync($"No results for '{query}'.");
            logger.LogInformation("/search {Query} => 0 results", query);
            return;
        }

        var embeds = new List<EmbedBuilder>();
        foreach (var item in result.data)
        {
            var eb = new EmbedBuilder()
                .WithTitle(item.name)
                .WithUrl(item.url)
                .WithColor(new Color(0, 128, 128));
            var preview = item.preview_html.content;
            if (!string.IsNullOrWhiteSpace(preview))
            {
                var cleaned = PreviewCleaner.Clean(preview);
                if (!string.IsNullOrWhiteSpace(cleaned))
                    eb.WithDescription(cleaned.Length > 500 ? cleaned[..500] + "…" : cleaned);
            }
            embeds.Add(eb);
        }

        await command.FollowupAsync(
            embeds: [.. embeds.Select(e => e.Build())],
            text: result.Total > 5 ? $"Showing {embeds.Count} of {result.Total} results" : null
        );
        logger.LogInformation(
            "/search {Query} => {Shown}/{Total}",
            query,
            embeds.Count,
            result.Total
        );
    }
}
