using Discord;
using Discord.WebSocket;
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
                    throw new InvalidOperationException("Expected input to be defined");

                switch (commandName)
                {
                    case "roll":
                        await interaction.RespondAsync(await HandleRollCommand(userInput, userName));
                        break;
                }
            };
        }

        private static async Task<string> HandleRollCommand(string userInput, string username)
        {
            if (!ParseDiceUserInput.TryParse(userInput, out var parsed, out var parseError))
                return parseError!;

            var roller = new DiceRoller.DiceRoller(username);
            var lines = new List<string>();
            var rolls = new List<(int sides, int result, string? message)>();
            var total = 0;

            foreach (var sides in parsed.Dices)
            {
                var (value, msg) = await DiceRoller.DiceRoller.Roll(sides);
                rolls.Add((sides, value, string.IsNullOrWhiteSpace(msg) ? null : msg));
                total += value;
            }

            for (int i = 0; i < rolls.Count; i++)
            {
                var (sides, value, msg) = rolls[i];
                var prefix = rolls.Count == 1 ? string.Empty : $"Roll #{i + 1}: ";
                var line = $"{prefix}(d{sides}) => {value}";
                if (msg != null) line += $" **{msg}**";
                lines.Add(line);
            }

            var mod = parsed.Mod;
            if (rolls.Count > 1 || mod != 0)
            {
                var final = total + mod;
                if (mod != 0)
                {
                    var sign = mod > 0 ? "+" : "-";
                    lines.Add($"Modifier: {sign}{Math.Abs(mod)}");
                }
                lines.Add($"Total: {final}");
            }

            return string.Join("\n", lines);
        }
    }
}