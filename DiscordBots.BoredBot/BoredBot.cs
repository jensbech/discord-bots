using Discord;
using Discord.WebSocket;
using DiscordBots.BoredBot.DiceRoller;
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

        public static async Task<BoredBot> GetInstanceAsync(BotEnvironmentVariables envVars, SlashCommandBuilder[] commands)
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
            _client.SlashCommandExecuted += async (SocketSlashCommand interaction) =>
            {
                var commandName = interaction.CommandName;
                var userName = interaction.User.GlobalName ?? interaction.User.Username;
                var userInput = interaction.Data.Options?.FirstOrDefault()?.Value?.ToString();

                if (string.IsNullOrEmpty(userInput))
                {
                    throw new InvalidOperationException("Expected input to be defined");
                }

                Console.WriteLine("got command");

                switch (commandName)
                {
                    case "roll":
                        await interaction.RespondAsync(await HandleRollCommand(userInput, userName));
                        break;
                }
            };
        }

        private async Task<string> HandleRollCommand(string inputStringFromUser, string username)
        {
            DiceParseResult parsedInputResult = new() { Dices = new List<int>(), Mod = 0 };

            try
            {
                parsedInputResult = ParseDiceUserInput.Parse(inputStringFromUser);
            }
            catch (Exception error)
            {
                return error.Message;
            }

            var roller = new DiceRoller.DiceRoller(username);
            var resultsMessages = new List<string>();

            if (parsedInputResult.Dices.Count == 1)
            {
                var singleDie = parsedInputResult.Dices[0];
                var (rollResult, message) = await roller.Roll(singleDie);

                var mod = parsedInputResult.Mod;
                var finalResult = rollResult + mod;

                var singleLine = $"(d{singleDie}) => {rollResult}";
                if (mod != 0)
                {
                    var sign = mod > 0 ? "+" : "-";
                    singleLine += $" {sign}{Math.Abs(mod)} = {finalResult}";
                }

                if (!string.IsNullOrEmpty(message))
                {
                    singleLine += $"\n{message}";
                }

                resultsMessages.Add(singleLine);
            }
            else
            {
                resultsMessages.Add($"You rolled {parsedInputResult.Dices.Count} dice!");

                var sum = 0;
                var rollCount = 1;

                foreach (var dieInput in parsedInputResult.Dices)
                {
                    var (rollResult, message) = await roller.Roll(dieInput);
                    sum += rollResult;

                    var prefix = $"Roll #{rollCount}: ";
                    var suffix = !string.IsNullOrEmpty(message) ? $" **{message}**" : "";

                    resultsMessages.Add($"{prefix}(d{dieInput}) => {rollResult}{suffix}");
                    rollCount++;
                }

                var mod = parsedInputResult.Mod;
                var finalResult = sum + mod;
                if (mod != 0)
                {
                    var sign = mod > 0 ? "+" : "-";
                    resultsMessages.Add($"Result: {sum} {sign} {Math.Abs(mod)} = {finalResult}");
                }
                else
                {
                    resultsMessages.Add($"**Final result: {sum}**");
                }
            }

            return string.Join("\n", resultsMessages);
        }
    }
}