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
                case "help":
                {
                    await command.RespondAsync("Available commands: /roll, /search, /chat, /help");
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
    }
}
