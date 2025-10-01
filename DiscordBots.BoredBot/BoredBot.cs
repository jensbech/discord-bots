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
        private static BoredBot? instance;

        private BoredBot(string token, SlashCommandBuilder[] commands, ILogger<BoredBot> logger)
            : base(token, commands, logger) { }

        public static async Task<BoredBot> GetOrCreateInstance(
            BotEnvironmentVariables envVars,
            SlashCommandBuilder[] commands,
            ILogger<BoredBot> logger
        )
        {
            if (instance == null)
            {
                instance = new BoredBot(envVars.DiscordBotToken, commands, logger);
                await instance.InitializeAsync("Bored Bot");
            }
            return instance;
        }

        public static BoredBot Instance => instance ?? throw new Exception("");

        private IBookStackClient? _bookStack;
        private IOpenAIClient? _openAI;
        private IReadOnlyDictionary<string, ISlashCommandHandler> _handlers = new Dictionary<
            string,
            ISlashCommandHandler
        >(StringComparer.OrdinalIgnoreCase);

        public void SetBookStackClient(IBookStackClient client) => _bookStack = client;

        public void SetOpenAIClient(IOpenAIClient client) => _openAI = client;

        internal DiscordSocketClient GetClient() => _client;

        protected override async Task OnSlashCommandAsync(SocketSlashCommand command)
        {
            if (_bookStack is null || _openAI is null)
                throw new InvalidOperationException("Dependencies not set before usage.");

            var handlers = new List<ISlashCommandHandler>
            {
                new Roll(),
                new Search(_bookStack),
                new Chat(_openAI),
                new Ask(_bookStack, _openAI),
                new Help(),
            };
            _handlers = handlers.ToDictionary(h => h.Name, StringComparer.OrdinalIgnoreCase);

            if (_handlers.TryGetValue(command.CommandName, out var handler))
            {
                await handler.HandleAsync(command, _logger);
                return;
            }
            await command.RespondAsync("Unknown command.", ephemeral: true);
            _logger.LogSlashError(command, "Unhandled command");
        }
    }
}
