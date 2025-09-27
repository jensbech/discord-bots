using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;

namespace DiscordBots.BoredBot.DiceRoller.Utils
{
    public static partial class ParseDiceUserInput
    {
        private static readonly HashSet<int> AllowedDiceSides =
        [
            Dice.Four, Dice.Six, Dice.Ten, Dice.Twelve, Dice.Twenty, Dice.Hundred
        ];

        private const int MaxDiceGroups = 30;
        private const int MaxTotalDice = 200;
        private const int MaxModifierMagnitude = 10_000;

        public static DiceParseResult Parse(string input)
        {
            if (!TryParse(input, out var result, out var error))
                throw new ArgumentException(error);
            return result!;
        }

        public static bool TryParse(string input, [NotNullWhen(true)] out DiceParseResult? result, out string? error)
        {
            result = null;
            error = null;

            if (string.IsNullOrWhiteSpace(input))
            {
                error = "Input must be a non-empty string.";
                return false;
            }

            var clean = input.ToLowerInvariant().Trim();
            clean = StripNoise(clean);

            if (RegexNumber().IsMatch(clean))
            {
                if (!int.TryParse(clean, out var sides))
                {
                    error = $"Unable to parse number '{clean}'.";
                    return false;
                }
                if (!AllowedDiceSides.Contains(sides))
                {
                    error = $"Allowed dice sides are {string.Join(", ", AllowedDiceSides)}. Received: {sides}";
                    return false;
                }
                result = new DiceParseResult { Dices = [sides], Mod = 0 };
                return true;
            }

            var rawTokens = RegexToken().Matches(clean).Cast<Match>().Select(m => m.Value).ToArray();
            if (rawTokens.Length == 0)
            {
                error = "Unable to parse any dice or modifiers. Examples: '3d6+5', 'd20-2', '2d8+1d6+4'.";
                return false;
            }

            if (rawTokens.Length > MaxDiceGroups + 50)
            {
                error = "Expression too long / complex.";
                return false;
            }

            var diceList = new List<int>();
            var modifierTotal = 0;
            var diceGroupCount = 0;

            foreach (var tok in rawTokens)
            {
                if (tok.Contains('d'))
                {
                    diceGroupCount++;
                    if (diceGroupCount > MaxDiceGroups)
                    {
                        error = $"Too many dice groups (>{MaxDiceGroups}).";
                        return false;
                    }

                    var parts = tok.Split('d');
                    if (parts.Length != 2)
                    {
                        error = $"Malformed dice token '{tok}'.";
                        return false;
                    }

                    var countPart = parts[0];
                    var sidesPart = parts[1];

                    int diceCount = 1;
                    if (!string.IsNullOrEmpty(countPart))
                    {
                        if (!int.TryParse(countPart, out diceCount) || diceCount <= 0)
                        {
                            error = $"Invalid dice count in '{tok}'.";
                            return false;
                        }
                    }

                    if (!int.TryParse(sidesPart, out var sides) || sides <= 0)
                    {
                        error = $"Invalid dice sides in '{tok}'.";
                        return false;
                    }

                    if (!AllowedDiceSides.Contains(sides))
                    {
                        error = $"Allowed dice sides are {string.Join(", ", AllowedDiceSides)}. Received: {sides}";
                        return false;
                    }

                    if (diceList.Count + diceCount > MaxTotalDice)
                    {
                        error = $"Too many dice requested (>{MaxTotalDice} total).";
                        return false;
                    }

                    for (int i = 0; i < diceCount; i++)
                        diceList.Add(sides);
                }
                else
                {
                    if (!int.TryParse(tok, out var value))
                    {
                        error = $"Invalid modifier '{tok}'.";
                        return false;
                    }
                    if (Math.Abs(value) > MaxModifierMagnitude)
                    {
                        error = $"Modifier magnitude too large (> {MaxModifierMagnitude}).";
                        return false;
                    }
                    modifierTotal += value;
                }
            }

            result = new DiceParseResult { Dices = diceList, Mod = modifierTotal };
            return true;
        }

        private static string StripNoise(string value)
        {
            value = RegexNoiseWords().Replace(value, "");
            value = RegexWhitespace().Replace(value, "");
            return value;
        }

        [GeneratedRegex(@"^\d+$")]
        private static partial Regex RegexNumber();
        [GeneratedRegex(@"\b(roll|dice|die)\b")]
        private static partial Regex RegexNoiseWords();
        [GeneratedRegex(@"\s+")]
        private static partial Regex RegexWhitespace();
        [GeneratedRegex(@"(\d*d\d+|[+\-]\d+)")]
        private static partial Regex RegexToken();
    }
}