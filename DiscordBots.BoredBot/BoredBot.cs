using Discord;
using Discord.WebSocket;
using DiscordBots.BoredBot.Dice;
using DiscordBots.Core;
using DiscordBots.Core.Logging;
using Microsoft.Extensions.Logging;

namespace DiscordBots.BoredBot
{
    public class BoredBot : DiscordBot
    {
        private static BoredBot? _instance;

        private BoredBot(
            string token,
            string applicationId,
            SlashCommandBuilder[] commands,
            ILogger<BoredBot> logger
        )
            : base(token, applicationId, commands, logger)
        {
            UseCommand();
        }

        public static async Task<BoredBot> GetInstanceAsync(
            BotEnvironmentVariables envVars,
            SlashCommandBuilder[] commands,
            ILogger<BoredBot> logger
        )
        {
            if (_instance == null)
            {
                _instance = new BoredBot(
                    envVars.DiscordBotToken,
                    envVars.ApplicationId,
                    commands,
                    logger
                );
                await _instance.InitializeAsync("Bored Bot");
            }
            return _instance;
        }

        private void UseCommand()
        {
            _client.SlashCommandExecuted += async interaction =>
            {
                var commandName = interaction.CommandName;
                var user = interaction.User;

                if (interaction.Channel is not ISocketMessageChannel channel)
                {
                    _logger.LogWarning(
                        "Slash command /{Command} received but channel was null",
                        commandName
                    );
                    await interaction.RespondAsync(
                        "Unexpected error determining channel",
                        ephemeral: true
                    );
                    return;
                }

                var guildName = (channel as SocketGuildChannel)?.Guild?.Name ?? "DM";
                var channelName = channel.Name;
                var textInput = interaction.Data.Options?.FirstOrDefault()?.Value?.ToString();

                switch (commandName)
                {
                    case "roll":
                    {
                        if (string.IsNullOrWhiteSpace(textInput))
                        {
                            await interaction.RespondAsync(
                                "Input required for /roll. Example: d20 or 2d8+4",
                                ephemeral: true
                            );
                            _logger.LogSlashError(interaction, "Missing dice expression");
                            break;
                        }
                        if (Roller.TryHandleRollCommand(textInput, out var resultMessage))
                        {
                            await interaction.RespondAsync(resultMessage);
                            _logger.LogSlash(interaction, resultMessage);
                        }
                        else
                        {
                            await interaction.RespondAsync(resultMessage, ephemeral: true);
                            _logger.LogSlashError(interaction, resultMessage);
                        }
                        break;
                    }
                    case "help":
                    {
                        await interaction.RespondAsync("Available commands: /roll, /help");
                        _logger.LogSlash(interaction);
                        break;
                    }
                    default:
                    {
                        await interaction.RespondAsync("Unknown command.", ephemeral: true);
                        _logger.LogSlashError(interaction, "Unhandled command");
                        break;
                    }
                }
            };
        }
    }
}
