using Discord;
using DiscordBots.BoredBot.DiceRoller.Utils;
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
                            string resultMessage = HandleRollCommand(textInput);
                            await interaction.RespondAsync(resultMessage);
                        }
                        break;
                }
            };
        }

        private static string HandleRollCommand(string textInput)
        {
            if (
                !ParseDiceUserInput.TryParseUserInput(
                    textInput,
                    out var parsedUserInput,
                    out var parseError
                )
            )
            {
                return parseError ?? throw new Exception("Dice parsing error is null");
            }
            var diceRolls = new List<(int sides, int result, string? message)>();
            var sumOfAllRolls = 0;

            foreach (var sides in parsedUserInput.Dices)
            {
                var (rollResult, rollResultMessage) = DiceRoller.DiceRoller.Roll(sides);
                diceRolls.Add((sides, rollResult, rollResultMessage));
                sumOfAllRolls += rollResult;
            }

            return ConstructRollOutcomeMessageLines(
                diceRolls,
                sumOfAllRolls,
                parsedUserInput.Modifier
            );
        }

        private static string ConstructRollOutcomeMessageLines(
            IReadOnlyList<(int sides, int result, string? message)> diceRolls,
            int sumOfAllRolls,
            int rollModifier
        )
        {
            var diceRollResultLines = new List<string>();

            for (int i = 0; i < diceRolls.Count; i++)
            {
                var (sides, value, msg) = diceRolls[i];
                var prefix = diceRolls.Count == 1 ? string.Empty : $"Roll #{i + 1}: ";
                var line = $"{prefix}(d{sides}) => {value}";
                if (!string.IsNullOrWhiteSpace(msg))
                    line += $" **{msg}**";
                diceRollResultLines.Add(line);
            }

            if (diceRolls.Count > 1 || rollModifier != 0)
            {
                var finalSumWithModifier = sumOfAllRolls + rollModifier;

                if (rollModifier != 0)
                {
                    var sign = rollModifier > 0 ? "+" : "-";
                    diceRollResultLines.Add($"Modifier: {sign}{Math.Abs(rollModifier)}");
                }
                diceRollResultLines.Add($"Total: {finalSumWithModifier}");
            }

            return string.Join("\n", diceRollResultLines);
        }
    }
}
