using System.Text.RegularExpressions;

namespace DiscordBots.BoredBot.DiceRoller.Utils
{
    public static partial class ParseDiceUserInput
    {
        private static readonly HashSet<int> AllowedDiceSides =
        [
            Dice.Four, Dice.Six, Dice.Ten, Dice.Twelve, Dice.Twenty, Dice.Hundred
        ];

        public static DiceParseResult Parse(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                throw new ArgumentException("Input must be a non-empty string.");
            }

            var clean = input.ToLowerInvariant().Trim();
            clean = MyRegex1().Replace(clean, "");
            clean = MyRegex2().Replace(clean, "");

            if (MyRegex().IsMatch(clean))
            {
                var sides = int.Parse(clean);
                if (!AllowedDiceSides.Contains(sides))
                {
                    throw new ArgumentException($"Allowed dice sides are {string.Join(", ", AllowedDiceSides)}. Received: {sides}");
                }
                return new DiceParseResult { Dices = [sides], Mod = 0 };
            }

            var tokenRegex = MyRegex3();
            var tokens = tokenRegex.Matches(clean).Cast<Match>().Select(m => m.Value).ToArray();

            if (tokens.Length == 0)
            {
                throw new ArgumentException("Unable to parse any dice or modifiers. Examples: '3d6+5', 'd20-2', '2d8+1d6+4'.");
            }

            var dices = new List<int>();
            var mod = 0;

            foreach (var token in tokens)
            {
                if (token.Contains('d'))
                {
                    var parts = token.Split('d');
                    var countPart = parts[0];
                    var sidesPart = parts[1];

                    var diceCount = string.IsNullOrEmpty(countPart) ? 1 : int.Parse(countPart);
                    if (diceCount < 1) diceCount = 1;

                    var sides = int.Parse(sidesPart);
                    if (sides <= 0)
                    {
                        throw new ArgumentException($"Invalid number of sides: \"{sidesPart}\"");
                    }

                    if (!AllowedDiceSides.Contains(sides))
                    {
                        throw new ArgumentException($"Allowed dice sides are {string.Join(", ", AllowedDiceSides)}. Received: {sides}");
                    }

                    for (int i = 0; i < diceCount; i++)
                    {
                        dices.Add(sides);
                    }
                }
                else
                {
                    var parsedMod = int.Parse(token);
                    mod += parsedMod;
                }
            }

            return new DiceParseResult { Dices = dices, Mod = mod };
        }

        [GeneratedRegex(@"^\d+$")]
        private static partial Regex MyRegex();
        [GeneratedRegex(@"\b(roll|dice|die)\b")]
        private static partial Regex MyRegex1();
        [GeneratedRegex(@"\s+")]
        private static partial Regex MyRegex2();
        [GeneratedRegex(@"(\d*d\d+|[+\-]\d+)")]
        private static partial Regex MyRegex3();
    }
}