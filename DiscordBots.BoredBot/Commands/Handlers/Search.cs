using Discord;
using Discord.WebSocket;
using DiscordBots.BookStack;
using DiscordBots.BookStack.Interfaces;
using DiscordBots.BoredBot.Commands.Interfaces;
using Microsoft.Extensions.Logging;

namespace DiscordBots.BoredBot.Commands.Handlers
{
    internal sealed class Search(IBookStackClient bookStackClient) : ISlashCommandHandler
    {
        private readonly IBookStackClient _bookStackClient = bookStackClient;
        public string Name => "search";

        public async Task HandleAsync(SocketSlashCommand command, ILogger logger)
        {
            var query =
                command.Data.Options?.FirstOrDefault(o => o.Name == "query")?.Value?.ToString()
                ?? throw new Exception("Failed to determine use query");

            await command.DeferAsync();

            var result = await _bookStackClient.SearchAsync(query);

            if (result == null || result.Data.Count == 0)
            {
                await command.FollowupAsync($"No results for '{query}'.");
                logger.LogInformation("/search {Query} => 0 results", query);
                return;
            }

            var embeds = new List<EmbedBuilder>();
            foreach (var item in result.Data)
            {
                var embed = new EmbedBuilder()
                    .WithTitle(item.Name)
                    .WithUrl(item.Url)
                    .WithColor(new Color(0, 128, 128));
                var preview = item.PreviewHtml.Content;

                var cleaned = PreviewCleaner.Clean(preview);
                if (!string.IsNullOrWhiteSpace(cleaned))
                    embed.WithDescription(cleaned.Length > 500 ? cleaned[..500] + "…" : cleaned);
                embeds.Add(embed);
            }

            await command.FollowupAsync(
                embeds: [.. embeds.Select(e => e.Build())],
                text: result.Total > 5 ? $"Showing {embeds.Count} of {result.Total} results" : null
            );
        }
    }
}
