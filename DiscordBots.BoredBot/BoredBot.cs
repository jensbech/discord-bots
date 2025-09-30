using Discord;
using Discord.WebSocket;
using DiscordBots.BookStack;
using DiscordBots.BoredBot.Commands;
using DiscordBots.BoredBot.Commands.Handlers;
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
        private IReadOnlyDictionary<string, ISlashCommandHandler> _handlers = new Dictionary<
            string,
            ISlashCommandHandler
        >(StringComparer.OrdinalIgnoreCase);

        public void SetBookStackClient(IBookStackClient client) => _bookStack = client;

        public void SetOpenAIClient(IOpenAIClient client) => _openAI = client;

        protected override async Task OnSlashCommandAsync(SocketSlashCommand command)
        {
            if (_bookStack is null || _openAI is null)
                throw new InvalidOperationException("Dependencies not set before usage.");

            var list = new List<ISlashCommandHandler>
            {
                new RollHandler(),
                new Search(_bookStack),
                new ChatHandler(_openAI),
                new AskHandler(_bookStack, _openAI),
                new HelpHandler(),
            };
            _handlers = list.ToDictionary(h => h.Name, StringComparer.OrdinalIgnoreCase);

            if (_handlers.TryGetValue(command.CommandName, out var handler))
            {
                await handler.HandleAsync(command, _logger);
                return;
            }
            await command.RespondAsync("Unknown command.", ephemeral: true);
            _logger.LogSlashError(command, "Unhandled command");
        }

        private async Task<(string Url, string? Text)> FetchPageAsync(string url)
        {
            if (_bookStack is null)
                return (url, null);
            try
            {
                var text = await _bookStack.GetPageHtmlAsync(url);
                return (url, text);
            }
            catch
            {
                return (url, null);
            }
        }
    }
}
