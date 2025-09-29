using Discord;
using Discord.WebSocket;
using DiscordBots.BookStack;
using DiscordBots.BoredBot.Dice;
using DiscordBots.Core;
using DiscordBots.Core.Logging;
using DiscordBots.OpenAI;
using Microsoft.Extensions.Logging;

namespace DiscordBots.BoredBot
{
    public partial class BoredBot : DiscordBot
    {
        private static BoredBot? _instance;

        private BoredBot(string token, SlashCommandBuilder[] commands, ILogger<BoredBot> logger)
            : base(token, commands, logger) { }

        public static async Task<BoredBot> GetOrCreateInstance(
            BotEnvironmentVariables envVars,
            SlashCommandBuilder[] commands,
            ILogger<BoredBot> logger
        )
        {
            if (_instance == null)
            {
                _instance = new BoredBot(envVars.DiscordBotToken, commands, logger);
                await _instance.InitializeAsync("Bored Bot");
            }
            return _instance;
        }

        public static BoredBot? Instance => _instance;

        private IBookStackClient? _bookStack;
        private IOpenAIClient? _openAI;

        public void SetBookStackClient(IBookStackClient client) => _bookStack = client;

        public void SetOpenAIClient(IOpenAIClient client) => _openAI = client;

        protected override async Task OnSlashCommandAsync(SocketSlashCommand command)
        {
            switch (command.CommandName)
            {
                case "roll":
                {
                    var input = GetStringOption(command, "input");
                    if (string.IsNullOrWhiteSpace(input))
                    {
                        await command.RespondAsync(
                            "Input required for /roll. Example: d20 or 2d8+4",
                            ephemeral: true
                        );
                        _logger.LogSlashError(command, "Missing dice expression");
                        return;
                    }
                    if (Roller.TryHandleRollCommand(input, out var resultMessage))
                    {
                        await command.RespondAsync(resultMessage);
                        _logger.LogSlash(command, resultMessage);
                    }
                    else
                    {
                        await command.RespondAsync(resultMessage, ephemeral: true);
                        _logger.LogSlashError(command, resultMessage);
                    }
                    break;
                }
                case "search":
                {
                    var query = GetStringOption(command, "query");
                    if (string.IsNullOrWhiteSpace(query))
                    {
                        await command.RespondAsync("Query required for /search.", ephemeral: true);
                        _logger.LogSlashError(command, "Missing query");
                        break;
                    }
                    if (_bookStack is null)
                    {
                        await command.RespondAsync("Search service not ready.", ephemeral: true);
                        _logger.LogSlashError(command, "BookStack client not set");
                        break;
                    }
                    await command.DeferAsync();
                    var result = await _bookStack!.SearchAsync(query, count: 5);
                    if (result == null || result.Data.Count == 0)
                    {
                        await command.FollowupAsync($"No results for '{query}'.");
                        _logger.LogSlash(command, "0 results");
                        break;
                    }
                    var embeds = new List<EmbedBuilder>();
                    foreach (var item in result.Data.Take(5))
                    {
                        var eb = new EmbedBuilder()
                            .WithTitle(item.Name)
                            .WithUrl(item.Url)
                            .WithColor(new Color(0, 128, 128));
                        var preview = item.Preview_Html.Content;
                        if (!string.IsNullOrWhiteSpace(preview))
                        {
                            var cleaned = CleanPreview(preview);
                            if (!string.IsNullOrWhiteSpace(cleaned))
                                eb.WithDescription(
                                    cleaned.Length > 500 ? cleaned[..500] + "…" : cleaned
                                );
                        }
                        embeds.Add(eb);
                    }
                    await command.FollowupAsync(
                        embeds: [.. embeds.Select(e => e.Build())],
                        text: result.Total > 5
                            ? $"Showing {embeds.Count} of {result.Total} results"
                            : null
                    );
                    _logger.LogSlash(command, $"Returned {embeds.Count}/{result.Total}");
                    break;
                }
                case "chat":
                {
                    var question = GetStringOption(command, "question");
                    if (string.IsNullOrWhiteSpace(question))
                    {
                        await command.RespondAsync("Question required for /chat.", ephemeral: true);
                        _logger.LogSlashError(command, "Missing question");
                        break;
                    }
                    if (_openAI is null)
                    {
                        await command.RespondAsync("Chat service not ready.", ephemeral: true);
                        _logger.LogSlashError(command, "OpenAI client not set");
                        break;
                    }
                    await command.DeferAsync();
                    var response = await _openAI.ChatAsync(question);
                    if (string.IsNullOrWhiteSpace(response))
                    {
                        await command.FollowupAsync(
                            "Sorry, I couldn't generate a response. Please try again."
                        );
                        _logger.LogSlash(command, "Empty response from OpenAI");
                        break;
                    }
                    await command.FollowupAsync(response);
                    _logger.LogSlash(command, "Chat response provided");
                    break;
                }
                case "ask":
                {
                    var question = GetStringOption(command, "question");

                    if (string.IsNullOrWhiteSpace(question))
                    {
                        await command.RespondAsync("Question required for /ask.", ephemeral: true);
                        _logger.LogSlashError(command, "Missing question");
                        break;
                    }

                    await command.DeferAsync();
                    try
                    {
                        var searchQuery = DeriveSearchQuery(question);

                        var search = await _bookStack.SearchAsync(searchQuery, count: 5);
                        
                        if (search is null || search.Data.Count == 0)
                        {
                            await command.FollowupAsync(
                                $"No knowledge base results for '{searchQuery}'."
                            );
                            _logger.LogSlash(command, "ask no results");
                            break;
                        }

                        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                        var results = new List<(string Url, string? Text)>();
                        foreach (var item in search.Data.Take(5))
                        {
                            var url = item.Url;
                            var result = await FetchPageAsync(url, cts.Token);
                            results.Add(result);
                            await Task.Delay(100, cts.Token);
                        }
                        var docs = new List<string>();
                        foreach (var item in search.Data.Take(5))
                        {
                            var (Url, Text) = results.FirstOrDefault(r => r.Url == item.Url);

                            if (Text is null)
                                continue;
                            var preview = CleanPreview(item.Preview_Html.Content ?? string.Empty);
                            var header = $"Title: {item.Name}\nURL: {item.Url}";
                            if (!string.IsNullOrWhiteSpace(preview))
                            {
                                preview = preview.Length > 400 ? preview[..400] + "…" : preview;
                                header += $"\nPreview: {preview}";
                            }
                            string body;
                            if (Text.Length > 2500)
                                body = Text[..2500] + "…";
                            else
                                body = Text;
                            docs.Add(header + "\n\n" + body);
                        }
                        if (docs.Count == 0)
                        {
                            await command.FollowupAsync(
                                "Couldn't retrieve page contents to answer."
                            );
                            _logger.LogSlash(command, "ask empty docs");
                            break;
                        }

                        var answer = await _openAI.ChatWithContextAsync(question, docs);
                        if (string.IsNullOrWhiteSpace(answer))
                        {
                            await command.FollowupAsync("AI couldn't form an answer from docs.");
                            _logger.LogSlash(command, "ask no answer");
                            break;
                        }

                        var lines = answer.Split('\n');
                        lines = lines
                            .Where(l =>
                                !l.TrimStart()
                                    .StartsWith("Sources:", StringComparison.OrdinalIgnoreCase)
                            )
                            .ToArray();
                        answer = string.Join('\n', lines).Trim();

                        static IEnumerable<string> SplitForDiscord(string text)
                        {
                            const int maxLen = 1900;
                            if (text.Length <= maxLen)
                            {
                                yield return text;
                                yield break;
                            }
                            var paragraphs = text.Split("\n\n", StringSplitOptions.None);
                            var current = new System.Text.StringBuilder();
                            foreach (var p in paragraphs)
                            {
                                var block = p.TrimEnd();
                                if (
                                    current.Length + block.Length + 2 > maxLen
                                    && current.Length > 0
                                )
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

                        var chunks = SplitForDiscord(answer).ToList();
                        for (int i = 0; i < chunks.Count; i++)
                        {
                            if (i == 0)
                                await command.FollowupAsync(chunks[i]);
                            else
                                await command.FollowupAsync(chunks[i]);
                        }

                        var sourceSummary = string.Join(
                            "; ",
                            search.Data.Take(5).Select(s => s.Name + "=" + s.Url)
                        );
                        _logger.LogSlash(command, $"ask answered sources: {sourceSummary}");
                    }
                    catch (Exception ex)
                    {
                        await command.FollowupAsync("Unexpected error processing /ask.");
                        _logger.LogSlashError(command, $"ask exception {ex.Message}");
                    }
                    break;
                }
                case "help":
                {
                    await command.RespondAsync(
                        "Available commands: /roll, /search, /chat, /ask, /help"
                    );
                    _logger.LogSlash(command);
                    break;
                }
                default:
                {
                    await command.RespondAsync("Unknown command.", ephemeral: true);
                    _logger.LogSlashError(command, "Unhandled command");
                    break;
                }
            }
        }

        private static string CleanPreview(string preview)
        {
            if (string.IsNullOrWhiteSpace(preview))
                return string.Empty;
            var cleaned = preview
                .Replace("<strong>", "**", StringComparison.OrdinalIgnoreCase)
                .Replace("</strong>", "**", StringComparison.OrdinalIgnoreCase)
                .Replace("<u>", "__", StringComparison.OrdinalIgnoreCase)
                .Replace("</u>", "__", StringComparison.OrdinalIgnoreCase);
            cleaned = System.Text.RegularExpressions.Regex.Replace(
                cleaned,
                "<img[^>]*>",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
            cleaned = MyRegex1().Replace(cleaned, "");
            cleaned = MyRegex().Replace(cleaned, "\n");
            cleaned = MyRegex2().Replace(cleaned, "\n");
            return cleaned.Trim();
        }

        [System.Text.RegularExpressions.GeneratedRegex("\n{2,}")]
        private static partial System.Text.RegularExpressions.Regex MyRegex();

        [System.Text.RegularExpressions.GeneratedRegex("<[^>]+>")]
        private static partial System.Text.RegularExpressions.Regex MyRegex1();

        [System.Text.RegularExpressions.GeneratedRegex("\\s+\\n")]
        private static partial System.Text.RegularExpressions.Regex MyRegex2();

        private async Task<(string Url, string? Text)> FetchPageAsync(
            string url,
            CancellationToken ct
        )
        {
            if (_bookStack is null)
                return (url, null);
            try
            {
                var text = await _bookStack.GetPageTextAsync(url, ct);
                return (url, text);
            }
            catch
            {
                return (url, null);
            }
        }

        private static string DeriveSearchQuery(string question)
        {
            if (string.IsNullOrWhiteSpace(question))
                return question;
            var q = question.Trim();
            var lower = q.ToLowerInvariant();
            string[] prefixes =
            [
                "who is ",
                "who was ",
                "what is ",
                "what was ",
                "tell me about ",
                "give me information about ",
                "describe ",
                "hva er ",
                "hvem er ",
                "hvem var ",
                "fortell meg om ",
            ];
            foreach (var p in prefixes)
            {
                if (lower.StartsWith(p))
                {
                    q = q[p.Length..];
                    break;
                }
            }
            q = q.Trim().TrimEnd('?').Trim();
            var parts = q.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            );
            if (parts.Length > 6)
            {
                q = string.Join(' ', parts.Take(6));
            }
            return string.IsNullOrWhiteSpace(q) ? question : q;
        }
    }
}
