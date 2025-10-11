using Discord;
using Discord.WebSocket;
using DiscordBots.Core.Logging;
using Microsoft.Extensions.Logging;

namespace DiscordBots.Core;

public abstract class BaseDiscordBot(string token, SlashCommandBuilder[] commands, ILogger logger)
{
    private DiscordSocketClient DiscordSocketClient { get; } =
        new(
            new DiscordSocketConfig
            {
                GatewayIntents =
                    GatewayIntents.Guilds
                    | GatewayIntents.GuildMessages
                    | GatewayIntents.MessageContent
            }
        );

    public DiscordSocketClient DiscordClient => DiscordSocketClient;

    private async Task LoginAsync()
    {
        if (string.IsNullOrEmpty(token))
            throw new InvalidOperationException("No token provided when attempting to log in bot");

        try
        {
            await DiscordSocketClient.LoginAsync(TokenType.Bot, token);
            await DiscordSocketClient.StartAsync();
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
            var commandData = commands
                .Select(cmd => cmd.Build())
                .ToArray<ApplicationCommandProperties>();
            await DiscordSocketClient.Rest.BulkOverwriteGlobalCommands(commandData);

            logger.LogInformation(
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

        logger.LogIncomingUserMessage(message);
        return Task.CompletedTask;
    }

    public async Task InitializeAsync(string botName)
    {
        logger.LogInformation("Initializing bot '{BotName}'...", botName);
        try
        {
            await LoginAsync();
            DiscordSocketClient.MessageReceived += LogIncomingMessage;

            DiscordSocketClient.Ready += async () =>
            {
                try
                {
                    await RegisterCommandsAsync();
                    logger.LogInformation("{BotName} is ready! :)", botName);
                }
                catch (Exception inner)
                {
                    logger.LogError(
                        inner,
                        "Initialization error (post-ready) for {BotName}",
                        botName
                    );
                }
            };
            DiscordSocketClient.SlashCommandExecuted += OnSlashCommandInternalAsync;
        }
        catch (Exception error)
        {
            logger.LogError(error, "Initialization error for {BotName}", botName);
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
            logger.LogError(
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

    protected virtual Task OnSlashCommandAsync(SocketSlashCommand command) => Task.CompletedTask;

    public static BotEnvironmentVariables EnsureEnvironmentVariables()
    {
        string[] requiredVars = ["DISCORD_BOT_TOKEN", "APPLICATION_ID"];
        var missing = requiredVars.Where(name => Environment.GetEnvironmentVariable(name) is null);
        var enumerable = missing as string[] ?? missing.ToArray();
        return enumerable.Length != 0
            ? throw new Exception($"Missing environment variables: {string.Join(", ", enumerable)}")
            : new BotEnvironmentVariables(Environment.GetEnvironmentVariable(requiredVars[0])!);
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
