namespace DiscordBots.BoredBot.Dice
{
    public class Roller
    {
        public static bool TryHandleRollCommand(string textInput, out string resultMessage)
        {
            resultMessage = string.Empty;
            if (!InputParser.Parse(textInput, out var parsedUserInput, out var parseError))
            {
                resultMessage = parseError ?? "Unknown dice parsing error.";
                return false;
            }

            var diceRolls = new List<(int sides, int result, string? message)>();
            var sumOfAllRolls = 0;

            foreach (var sides in parsedUserInput.Dices)
            {
                var (rollResult, rollResultMessage) = Roll(sides);
                diceRolls.Add((sides, rollResult, rollResultMessage));
                sumOfAllRolls += rollResult;
            }

            resultMessage = ConstructRollOutcomeMessageLines(
                diceRolls,
                sumOfAllRolls,
                parsedUserInput.Modifier
            );
            return true;
        }

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
            if (diceRolls.Count == 1 && rollModifier == 0)
            {
                var (sides, result, msg) = diceRolls[0];
                var critPart = string.IsNullOrWhiteSpace(msg) ? string.Empty : $" — **{msg}**";
                var emoji = GetResultEmoji(sides, result);
                return $"{emoji}  d{sides} = **{result}**{critPart}";
            }

            var lines = new List<string> { "🎲 Dice Rolls:" };

            int width = diceRolls.Max(r => r.sides.ToString().Length);
            for (int i = 0; i < diceRolls.Count; i++)
            {
                var (sides, value, msg) = diceRolls[i];
                var sideLabel = ($"d{sides}").PadLeft(width + 1);
                var msgPart = string.IsNullOrWhiteSpace(msg) ? string.Empty : $"  **{msg}**";
                var emoji = GetResultEmoji(sides, value);
                lines.Add($"{emoji}  {sideLabel} = {value}{msgPart}");
            }

            if (rollModifier != 0)
            {
                var sign = rollModifier > 0 ? "+" : "-";
                lines.Add($"Modifier: {sign}{Math.Abs(rollModifier)}");
            }

            var total = sumOfAllRolls + rollModifier;
            lines.Add($"═ Total: **{total}**");

            return string.Join("\n", lines);
        }

        private static string GetResultEmoji(int sides, int value)
        {
            if (sides <= 0)
                return "🎲";

            double pct = value / (double)sides;

            if (sides == Dice.Twenty)
            {
                if (value == 1)
                    return "💀";
                if (value == 20)
                    return "🌟";
            }

            return pct switch
            {
                <= 0.10 => "💢",
                <= 0.25 => "😖",
                <= 0.50 => "😐",
                <= 0.75 => "🙂",
                <= 0.90 => "😄",
                < 1.00 => "🔥",
                _ => "🏆",
            };
        }
    }
}
