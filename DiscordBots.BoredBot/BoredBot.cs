using Discord;
using DiscordBots.BoredBot.Dice;
using DiscordBots.Core;

namespace DiscordBots.BoredBot
{
    public class BoredBot : DiscordBot
    {
        private static BoredBot? _instance;

        private BoredBot(string token, string applicationId, SlashCommandBuilder[] commands)
            : base(token, applicationId, commands)
        {
            UseCommand();
        }

        public static async Task<BoredBot> GetInstanceAsync(
            BotEnvironmentVariables envVars,
            SlashCommandBuilder[] commands
        )
        {
            if (_instance == null)
            {
                _instance = new BoredBot(envVars.DiscordBotToken, envVars.ApplicationId, commands);
                await _instance.InitializeAsync("Bored Bot");
            }
            return _instance;
        }

        private void UseCommand()
        {
            _client.SlashCommandExecuted += static async interaction =>
            {
                var commandName = interaction.CommandName;
                // var discordUserName = interaction.User.GlobalName ?? interaction.User.Username;
                var textInput = interaction.Data.Options?.FirstOrDefault()?.Value?.ToString();

                if (string.IsNullOrEmpty(textInput))
                    throw new InvalidOperationException("Expected input to be defined");

                switch (commandName)
                {
                    case "roll":
                        {
                            string resultMessage = Roller.HandleRollCommand(textInput);
                            await interaction.RespondAsync(resultMessage);
                        }
                        break;
                }
            };
        }
    }
}
