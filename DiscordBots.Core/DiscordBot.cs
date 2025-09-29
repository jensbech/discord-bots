using Discord;
using Discord.WebSocket;
using DiscordBots.Core.Logging;
using Microsoft.Extensions.Logging;

namespace DiscordBots.Core
{
    public abstract class DiscordBot
    {
        private readonly string _token;
        protected readonly DiscordSocketClient _client;
        private readonly SlashCommandBuilder[] _commands;
        protected IReadOnlyList<SlashCommandBuilder> Commands => _commands;
        protected readonly ILogger _logger;

        protected DiscordBot(string token, SlashCommandBuilder[] commands, ILogger logger)
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
            _commands = commands;
            _logger = logger;
            _client.MessageReceived += LogIncomingMessage;
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
                _logger.LogInformation(
                    "Registered {CommandCount} global slash command(s)",
                    commandData.Length
                );
            }
            catch (Exception error)
            {
                throw new InvalidOperationException(
                    $"Failed to register slash commands: {error.Message}"
                );
            }
        }

        private Task LogIncomingMessage(SocketMessage message)
        {
            if (message.Author.IsBot)
                return Task.CompletedTask;
            _logger.LogIncomingUserMessage(message);
            return Task.CompletedTask;
        }

        protected async Task InitializeAsync(string botName)
        {
            _logger.LogInformation("Initializing bot '{BotName}'...", botName);
            try
            {
                await LoginAsync();

                _client.Ready += async () =>
                {
                    try
                    {
                        await RegisterCommandsAsync();
                        _logger.LogInformation("{BotName} is ready! :)", botName);
                    }
                    catch (Exception inner)
                    {
                        _logger.LogError(
                            inner,
                            "Initialization error (post-ready) for {BotName}",
                            botName
                        );
                    }
                };
                _client.SlashCommandExecuted += OnSlashCommandInternalAsync;
            }
            catch (Exception error)
            {
                _logger.LogError(error, "Initialization error for {BotName}", botName);
            }
        }

        private async Task OnSlashCommandInternalAsync(SocketSlashCommand command)
        {
            try
            {
                await OnSlashCommandAsync(command);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unnhandled exception in slash command handler for /{Command}",
                    command.CommandName
                );

                var userInput = command.Data.Options is null
                    ? string.Empty
                    : string.Join(
                        " ",
                        command
                            .Data.Options.Select(o => o.Value?.ToString())
                            .Where(v => !string.IsNullOrWhiteSpace(v))
                    );
                var friendly = string.IsNullOrWhiteSpace(userInput)
                    ? $"Something went wrong handling /{command.CommandName}. Please try again or use /help."
                    : $"Something went wrong handling /{command.CommandName} {userInput}. Please try again or use /help.";

                if (!command.HasResponded)
                    await command.RespondAsync(friendly, ephemeral: true);
                else
                    await command.FollowupAsync(friendly, ephemeral: true);
            }
        }

        protected virtual Task OnSlashCommandAsync(SocketSlashCommand command) =>
            Task.CompletedTask;

        protected static string GetStringOption(SocketSlashCommand command, string name)
        {
            return command.Data.Options?.FirstOrDefault(o => o.Name == name)?.Value?.ToString()
                ?? throw new Exception(
                    $"Failed to construct input based on provided data: '{command}'"
                );
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
