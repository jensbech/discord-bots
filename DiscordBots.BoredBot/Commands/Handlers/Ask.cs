using System.Text;
using Discord.WebSocket;
using DiscordBots.BookStack;
using DiscordBots.OpenAI;
using Microsoft.Extensions.Logging;

namespace DiscordBots.BoredBot.Commands;

internal sealed class AskHandler(IBookStackClient bookStack, IOpenAIClient openAI)
    : ISlashCommandHandler
{
    private readonly IBookStackClient _bookStack = bookStack;
    private readonly IOpenAIClient _openAI = openAI;

    public string Name => "ask";

    public async Task HandleAsync(SocketSlashCommand command, ILogger logger)
    {
        var question =
            command.Data.Options?.FirstOrDefault(o => o.Name == "question")?.Value?.ToString()
            ?? string.Empty;
        await command.DeferAsync();
        try
        {
            var search = await _bookStack.SearchAsync(question, count: 5);
            if (search is null || search.data.Count == 0)
            {
                await command.FollowupAsync($"No knowledge base results for '{question}'.");
                logger.LogInformation("/ask {Question} => no results", question);
                return;
            }

            var docs = new List<string>();
            foreach (var item in search.data)
            {
                var pageHtml = await _bookStack.GetPageHtmlAsync(item.url);
                if (pageHtml is null)
                    continue;

                var preview = PreviewCleaner.Clean(item.preview_html.content ?? string.Empty);
                var header = $"Title: {item.name}\nURL: {item.url}";
                if (!string.IsNullOrWhiteSpace(preview))
                {
                    var trimmedPrev = preview.Length > 400 ? preview[..400] + "…" : preview;
                    header += $"\nPreview: {trimmedPrev}";
                }
                var body = pageHtml.Length > 2500 ? pageHtml[..2500] + "…" : pageHtml;
                docs.Add(header + "\n\n" + body);
            }

            if (docs.Count == 0)
            {
                await command.FollowupAsync("Couldn't retrieve page contents to answer.");
                logger.LogInformation("/ask {Question} => empty docs", question);
                return;
            }

            var answer = await _openAI.ChatWithContextAsync(question, docs);
            if (string.IsNullOrWhiteSpace(answer))
            {
                await command.FollowupAsync("AI couldn't form an answer from docs.");
                logger.LogInformation("/ask {Question} => no AI answer", question);
                return;
            }

            answer = RemoveSourcesSection(answer);

            foreach (var chunk in SplitForDiscord(answer))
            {
                await command.FollowupAsync(chunk);
            }

            logger.LogInformation("/ask {Question} => answered", question);
        }
        catch (Exception ex)
        {
            await command.FollowupAsync("Unexpected error processing /ask.");
            logger.LogError(ex, "/ask {Question} exception", question);
        }
    }

    private static string RemoveSourcesSection(string answer)
    {
        var lines = answer.Split('\n');
        lines = lines
            .Where(l => !l.TrimStart().StartsWith("Sources:", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        return string.Join('\n', lines).Trim();
    }

    private static IEnumerable<string> SplitForDiscord(string text)
    {
        const int maxLen = 1900;
        var paragraphs = text.Split("\n\n", StringSplitOptions.None);
        var current = new StringBuilder();
        foreach (var p in paragraphs)
        {
            var block = p.TrimEnd();
            if (current.Length + block.Length + 2 > maxLen && current.Length > 0)
            {
                yield return current.ToString();
                current.Clear();
            }
            if (block.Length > maxLen)
            {
                int idx = 0;
                while (idx < block.Length)
                {
                    var take = Math.Min(maxLen, block.Length - idx);
                    var slice = block.Substring(idx, take);
                    yield return slice;
                    idx += take;
                }
            }
            else
            {
                if (current.Length > 0)
                    current.Append('\n').Append('\n');
                current.Append(block);
            }
        }
        if (current.Length > 0)
            yield return current.ToString();
    }
}
