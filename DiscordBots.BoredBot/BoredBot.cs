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
    public class BoredBot : BaseDiscordBot
    {
        private static BoredBot? _instance;
        private IBookStackClient? _bookStack;
        private IOpenAiClient? _openAi;
        
        public static BoredBot Instance => _instance ?? throw new Exception("");

        private BoredBot(string token, SlashCommandBuilder[] commands, ILogger<BoredBot> logger)
            : base(token, commands, logger) { }

        public static async Task<BoredBot> GetOrCreateInstance(
            BotEnvironmentVariables envVars,
            SlashCommandBuilder[] commands,
            ILogger<BoredBot> logger
        )
        {
            if (_instance != null) return _instance;
            
            _instance = new BoredBot(envVars.DiscordBotToken, commands, logger);
            await _instance.InitializeAsync("Bored Bot");
            return _instance;
        }
        
        private IReadOnlyDictionary<string, ISlashCommandHandler> Handlers { get; set; } = new Dictionary<
            string,
            ISlashCommandHandler
        >(StringComparer.OrdinalIgnoreCase);

        public void SetBookStackClient(IBookStackClient client) => _bookStack = client;

        public void SetOpenAiClient(IOpenAiClient client) => _openAi = client;

        internal DiscordSocketClient GetClient() => Client;

        protected override async Task OnSlashCommandAsync(SocketSlashCommand command)
        {
            if (_bookStack is null || _openAi is null)
                throw new InvalidOperationException("Dependencies not set before usage.");

            var handlers = new List<ISlashCommandHandler>
            {
                new Roll(),
                new Search(_bookStack),
                new Chat(_openAi),
                new Ask(_bookStack, _openAi),
                new Help(),
            };
            Handlers = handlers.ToDictionary(h => h.Name, StringComparer.OrdinalIgnoreCase);

            if (Handlers.TryGetValue(command.CommandName, out var handler))
            {
                await handler.HandleAsync(command, Logger);
                return;
            }
            await command.RespondAsync("Unknown command.", ephemeral: true);
            Logger.LogSlashError(command, "Unhandled command");
        }
    }
}
