using System.Text;
using Discord.WebSocket;
using DiscordBots.BookStack;
using DiscordBots.BookStack.Models;
using DiscordBots.OpenAI;
using Microsoft.Extensions.Logging;

namespace DiscordBots.BoredBot.Commands.Handlers;

internal sealed class Ask(IBookStackClient bookStack, IOpenAiClient openAi) : ISlashCommandHandler
{
    public string Name => "ask";

    public async Task HandleAsync(SocketSlashCommand command, ILogger logger)
    {
        var question =
            command.Data.Options?.FirstOrDefault(o => o.Name == "question")?.Value?.ToString()
            ?? throw new Exception("Failed to determine user 'ask' question");

        await command.DeferAsync();

        try
        {
            var response = await bookStack.SearchAsync(question);
            if (response == null)
            {
                await command.FollowupAsync($"No knowledge base results for '{question}'.");
                logger.LogInformation("/ask {Question} => no results", question);
                return;
            }

            var bookstackDocuments = ConstructBookStackDocs(response.Data);

            var openAiAnswer = await openAi.AskChat(question, bookstackDocuments);

            if (string.IsNullOrWhiteSpace(openAiAnswer))
            {
                await command.FollowupAsync("AI couldn't form an answer from docs.");
                logger.LogInformation("/ask {Question} => no AI answer", question);
                return;
            }

            foreach (var chunk in SplitForDiscord(openAiAnswer))
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

    private List<string> ConstructBookStackDocs(
        IReadOnlyList<BookStackSearchResponse.BookStackSearchResult> results
    )
    {
        var docs = new List<string>();
        foreach (var item in results)
        {
            var pageHtml = bookStack.GetPageHtmlAsync(item.Url).Result;
            if (pageHtml is null)
                continue;

            var preview = PreviewCleaner.Clean(item.PreviewHtml.Content);
            var header = $"Title: {item.Name}\nURL: {item.Url}";

            if (!string.IsNullOrWhiteSpace(preview))
            {
                var trimmedPrev = preview.Length > 400 ? preview[..400] + "…" : preview;
                header += $"\nPreview: {trimmedPrev}";
            }

            var body = pageHtml.Length > 2500 ? pageHtml[..2500] + "…" : pageHtml;
            docs.Add(header + "\n\n" + body);
        }
        return docs;
    }

    private static IEnumerable<string> SplitForDiscord(string text)
    {
        const int maxLen = 1900;
        var paragraphs = text.Split("\n\n");
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
