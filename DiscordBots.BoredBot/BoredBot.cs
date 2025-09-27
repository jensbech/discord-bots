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
    }
}
