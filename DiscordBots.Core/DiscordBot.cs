using Discord;
using Discord.WebSocket;
using DiscordBots.Core.Logging;
using Microsoft.Extensions.Logging;

namespace DiscordBots.Core
{
    public abstract class DiscordBot
    {
        private readonly string _token;
        protected readonly DiscordSocketClient Client;
        private readonly SlashCommandBuilder[] _commands;
        protected IReadOnlyList<SlashCommandBuilder> Commands => _commands;
        protected readonly ILogger Logger;

        protected DiscordBot(string token, SlashCommandBuilder[] commands, ILogger logger)
        {
            Client = new DiscordSocketClient(
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
            Logger = logger;
            Client.MessageReceived += LogIncomingMessage;
        }

        private async Task LoginAsync()
        {
            if (string.IsNullOrEmpty(_token))
                throw new InvalidOperationException(
                    "No token provided when attempting to log in bot"
                );

            try
            {
                await Client.LoginAsync(TokenType.Bot, _token);
                await Client.StartAsync();
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
                var commandData = _commands.Select(cmd => cmd.Build()).ToArray<ApplicationCommandProperties>();
                await Client.Rest.BulkOverwriteGlobalCommands(commandData);
                
                Logger.LogInformation(
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
            
            Logger.LogIncomingUserMessage(message);
            return Task.CompletedTask;
        }

        protected async Task InitializeAsync(string botName)
        {
            Logger.LogInformation("Initializing bot '{BotName}'...", botName);
            try
            {
                await LoginAsync();

                Client.Ready += async () =>
                {
                    try
                    {
                        await RegisterCommandsAsync();
                        Logger.LogInformation("{BotName} is ready! :)", botName);
                    }
                    catch (Exception inner)
                    {
                        Logger.LogError(
                            inner,
                            "Initialization error (post-ready) for {BotName}",
                            botName
                        );
                    }
                };
                Client.SlashCommandExecuted += OnSlashCommandInternalAsync;
            }
            catch (Exception error)
            {
                Logger.LogError(error, "Initialization error for {BotName}", botName);
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
                Logger.LogError(
                    ex,
                    "Unhandled exception in slash command handler for /{Command}",
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
            var enumerable = missing as string[] ?? missing.ToArray();
            if (enumerable.Any())
                throw new Exception($"Missing environment variables: {string.Join(", ", enumerable)}");
            return new BotEnvironmentVariables(
                Environment.GetEnvironmentVariable(requiredVars[0])!
            );
        }
    }

    public sealed class BotEnvironmentVariables
    {
        public string DiscordBotToken { get; }

        internal BotEnvironmentVariables(string discordBotToken)
        {
            DiscordBotToken = discordBotToken;
        }
    }
}
