using System.Text;
using Discord;
using Discord.WebSocket;
using DiscordBots.BookStack.Interfaces;
using DiscordBots.BookStack.Models;
using DiscordBots.OpenAI.Interfaces;
using DiscordBots.Scribe.Commands.Interfaces;
using Microsoft.Extensions.Logging;

namespace DiscordBots.Scribe.Commands.Handlers;

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
            List<string> docs = [];
            List<(string Title, string Url)> sources = [];
            if (response?.Data != null)
            {
                docs = await BuildDocsAsync(response.Data, sources);
            }

            if (docs.Count == 0)
            {
                logger.LogInformation(
                    "/ask {Question} => no initial results, attempting query reduction",
                    question
                );
                var reduced = await openAi.ReduceSearchQuery(question);
                if (
                    !string.IsNullOrWhiteSpace(reduced)
                    && !reduced.Equals(question, StringComparison.OrdinalIgnoreCase)
                )
                {
                    logger.LogInformation(
                        "/ask {Question} => reduced query: {Reduced}",
                        question,
                        reduced
                    );
                    var retryResponse = await bookStack.SearchAsync(reduced);
                    sources.Clear();
                    if (retryResponse?.Data != null)
                    {
                        docs = await BuildDocsAsync(retryResponse.Data, sources);
                    }
                    if (docs.Count == 0)
                    {
                        var noResMsg = await command.FollowupAsync(
                            $"No results for '{question}' (reduced to '{reduced}')."
                        );
                        TrySuppressEmbeds(noResMsg, logger);
                        logger.LogInformation(
                            "/ask {Question} => no results after reduction",
                            question
                        );
                        return;
                    }
                }
                else if (string.IsNullOrWhiteSpace(reduced))
                {
                    var failMsg = await command.FollowupAsync(
                        $"No results for '{question}'. (Query reduction failed)"
                    );
                    TrySuppressEmbeds(failMsg, logger);
                    logger.LogInformation(
                        "/ask {Question} => no results and reduction failed",
                        question
                    );
                    return;
                }
                else
                {
                    var noResMsg = await command.FollowupAsync($"No results for '{question}'.");
                    TrySuppressEmbeds(noResMsg, logger);
                    logger.LogInformation(
                        "/ask {Question} => no results even after identical reduction",
                        question
                    );
                    return;
                }
            }

            var answer = await openAi.AskChat(question, docs);

            if (string.IsNullOrWhiteSpace(answer))
            {
                var noAnsMsg = await command.FollowupAsync("No answer.");
                TrySuppressEmbeds(noAnsMsg, logger);
                logger.LogInformation("/ask {Question} => no AI answer", question);
                return;
            }

            foreach (var chunk in Split(answer))
            {
                var msg = await command.FollowupAsync(chunk);
                TrySuppressEmbeds(msg, logger);
            }



            logger.LogInformation("/ask {Question} => answered", question);
        }
        catch (Exception ex)
        {
            var errMsg = await command.FollowupAsync("Error processing /ask.");
            TrySuppressEmbeds(errMsg, logger);
            logger.LogError(ex, "/ask {Question} exception", question);
        }
    }

    private async Task<List<string>> BuildDocsAsync(
        IReadOnlyList<BookStackSearchResponse.BookStackSearchResult> results,
        List<(string Title, string Url)> sources
    )
    {
        const int maxParallel = 50;
        var semaphore = new SemaphoreSlim(maxParallel);

        var tasks = results.Select(async r =>
        {
            await semaphore.WaitAsync();
            try
            {
                var html = await bookStack.GetEntityHtmlAsync(r.Type, r.Id);
                return (Result: r, Html: html);
            }
            finally
            {
                semaphore.Release();
            }
        }).ToList();

        var fetched = await Task.WhenAll(tasks);

        var list = new List<string>();
        foreach (var item in fetched)
        {
            var r = item.Result;
            var html = item.Html;
            if (html is null)
                continue;
            var preview = PreviewCleaner.Clean(r.PreviewHtml.Content);
            if (sources.Count < 3)
            {
                sources.Add((r.Name, r.Url));
            }
            var header =
                $"Title: {r.Name}\nURL: <{r.Url}>"
                + (
                    string.IsNullOrWhiteSpace(preview)
                        ? string.Empty
                        : $"\nPreview: {(preview.Length > 400 ? preview[..400] + "…" : preview)}"
                );
            var body = html.Length > 2500 ? html[..2500] + "…" : html;
            list.Add(header + "\n\n" + body);
        }
        return list;
    }

    private static IEnumerable<string> Split(string text)
    {
        const int max = 1900;
        var parts = text.Split("\n\n");
        var sb = new StringBuilder();
        foreach (var p in parts)
        {
            var b = p.TrimEnd();
            if (sb.Length + b.Length + 2 > max && sb.Length > 0)
            {
                yield return sb.ToString();
                sb.Clear();
            }
            if (b.Length > max)
            {
                for (var i = 0; i < b.Length; i += max)
                    yield return b.Substring(i, Math.Min(max, b.Length - i));
            }
            else
            {
                if (sb.Length > 0)
                    sb.Append('\n').Append('\n');
                sb.Append(b);
            }
        }
        if (sb.Length > 0)
            yield return sb.ToString();
    }

    private static string ToMarkdownLink(string title, string url)
    {
        var safeTitle = title.Replace("[", "\\[").Replace("]", "\\]");
        return $"[{safeTitle}](<{url}>)";
    }

    private static void TrySuppressEmbeds(IUserMessage? msg, ILogger logger)
    {
        if (msg is null)
            return;
        try
        {
            msg.ModifyAsync(p => p.Flags = MessageFlags.SuppressEmbeds);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to suppress embeds for followup message {Id}", msg.Id);
        }
    }
}
