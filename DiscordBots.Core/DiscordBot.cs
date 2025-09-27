using Discord;
using Discord.WebSocket;

namespace DiscordBots.Core
{
    public abstract class DiscordBot
    {
        private readonly string _token;
        private readonly string _applicationId;
        protected readonly DiscordSocketClient _client;
        private readonly SlashCommandBuilder[] _commands;

        protected DiscordBot(string token, string applicationId, SlashCommandBuilder[] commands)
        {
            _client = new DiscordSocketClient(
                new DiscordSocketConfig
                {
                    GatewayIntents =
                        GatewayIntents.Guilds
                        | GatewayIntents.GuildMessages
                        | GatewayIntents.MessageContent,
                }
            );
            _token = token;
            _applicationId = applicationId;
            _commands = commands;
        }

        private async Task LoginAsync()
        {
            if (string.IsNullOrEmpty(_token))
                throw new InvalidOperationException(
                    "No token provided when attempting to log in bot"
                );

            try
            {
                await _client.LoginAsync(TokenType.Bot, _token);
                await _client.StartAsync();
            }
            catch (Exception error)
            {
                throw new InvalidOperationException($"Bot failed to log in: {error.Message}");
            }
        }

        private async Task RegisterCommandsAsync()
        {
            try
            {
                var commandData = _commands.Select(cmd => cmd.Build()).ToArray();
                await _client.Rest.BulkOverwriteGlobalCommands(commandData);
                Console.WriteLine($"Registered {commandData.Length} global slash command(s).");
            }
            catch (Exception error)
            {
                throw new InvalidOperationException(
                    $"Failed to register slash commands: {error.Message}"
                );
            }
        }

        protected async Task InitializeAsync(string botName)
        {
            Console.WriteLine($"Initializing bot '{botName}'...");
            try
            {
                await LoginAsync();

                _client.Ready += async () =>
                {
                    try
                    {
                        await RegisterCommandsAsync();
                        Console.WriteLine($"{botName} is ready!");
                    }
                    catch (Exception inner)
                    {
                        Console.WriteLine($"Initialization error (post-ready): {inner.Message}");
                    }
                };
            }
            catch (Exception error)
            {
                Console.WriteLine($"Initialization error: {error.Message}");
            }
        }

        public static BotEnvironmentVariables EnsureEnvironmentVariables()
        {
            string[] requiredVars = ["DISCORD_BOT_TOKEN", "APPLICATION_ID"];
            var missing = requiredVars.Where(name =>
                Environment.GetEnvironmentVariable(name) is null
            );

            if (missing.Any())
                throw new Exception($"Missing environment variables: {string.Join(", ", missing)}");

            return new BotEnvironmentVariables(
                Environment.GetEnvironmentVariable(requiredVars[0])!,
                Environment.GetEnvironmentVariable(requiredVars[1])!
            );
        }
    }

    public sealed class BotEnvironmentVariables
    {
        public string DiscordBotToken { get; }
        public string ApplicationId { get; }

        internal BotEnvironmentVariables(string discordBotToken, string applicationId)
        {
            DiscordBotToken = discordBotToken;
            ApplicationId = applicationId;
        }
    }
}
