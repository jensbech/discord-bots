using Discord;
using Discord.WebSocket;
using DiscordBots.BookStack;
using DiscordBots.BoredBot.Dice;
using DiscordBots.Core;
using DiscordBots.Core.Logging;
using Microsoft.Extensions.Logging;

namespace DiscordBots.BoredBot
{
    public class BoredBot : DiscordBot
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

        public void SetBookStackClient(IBookStackClient client) => _bookStack = client;

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
                        embeds: embeds.Select(e => e.Build()).ToArray(),
                        text: result.Total > 5
                            ? $"Showing {embeds.Count} of {result.Total} results"
                            : null
                    );
                    _logger.LogSlash(command, $"Returned {embeds.Count}/{result.Total}");
                    break;
                }
                case "help":
                {
                    await command.RespondAsync("Available commands: /roll, /help");
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
            // Basic replacements similar to python version
            var cleaned = preview
                .Replace("<strong>", "**", StringComparison.OrdinalIgnoreCase)
                .Replace("</strong>", "**", StringComparison.OrdinalIgnoreCase)
                .Replace("<u>", "__", StringComparison.OrdinalIgnoreCase)
                .Replace("</u>", "__", StringComparison.OrdinalIgnoreCase);
            // Strip <img ...>
            cleaned = System.Text.RegularExpressions.Regex.Replace(
                cleaned,
                "<img[^>]*>",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
            // Remove html tags that remain (very light sanitizer)
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, "<[^>]+>", "");
            // Collapse whitespace
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, "\n{2,}", "\n");
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, "\\s+\\n", "\n");
            return cleaned.Trim();
        }
    }
}
