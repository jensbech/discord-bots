namespace DiscordBots.BoredBot.Dice
{
    public class Roller
    {
        public static string HandleRollCommand(string textInput)
        {
            if (!InputParser.Parse(textInput, out var parsedUserInput, out var parseError))
            {
                return parseError ?? throw new Exception("Dice parsing error is null");
            }
            var diceRolls = new List<(int sides, int result, string? message)>();
            var sumOfAllRolls = 0;

            foreach (var sides in parsedUserInput.Dices)
            {
                var (rollResult, rollResultMessage) = Roll(sides);
                diceRolls.Add((sides, rollResult, rollResultMessage));
                sumOfAllRolls += rollResult;
            }

            return ConstructRollOutcomeMessageLines(
                diceRolls,
                sumOfAllRolls,
                parsedUserInput.Modifier
            );
        }

        public static (int RollResult, string? Message) Roll(int diceInput)
        {
            var allowedDice = new[]
            {
                Dice.Four,
                Dice.Six,
                Dice.Ten,
                Dice.Twelve,
                Dice.Twenty,
                Dice.Hundred,
            };

            if (!allowedDice.Contains(diceInput))
                throw new ArgumentException($"Invalid dice type: {diceInput}");

            var (outcome, crit) = GetSingleDiceRollOutcome(diceInput);

            if (crit.Failure || crit.Success)
            {
                return (
                    outcome,
                    crit.Success ? HandleCritical(Critical.Success) : HandleCritical(Critical.Fail)
                );
            }
            return (outcome, null);
        }

        private static (int Outcome, (bool Failure, bool Success) Crit) GetSingleDiceRollOutcome(
            int dice
        )
        {
            int rollResult = new Random().Next(1, dice + 1);

            return dice switch
            {
                Dice.Twenty => (rollResult, (rollResult == 1, rollResult == 20)),
                _ => (rollResult, (false, false)),
            };
        }

        private static string HandleCritical(Critical critical)
        {
            return critical switch
            {
                Critical.Fail => "Critical FAIL!",
                Critical.Success => "Critical SUCCESS!",
                _ => string.Empty,
            };
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
                diceRollResultLines.Add($"Sum of all rolls: {finalSumWithModifier}");
            }

            return string.Join("\n", diceRollResultLines);
        }
    }
}
