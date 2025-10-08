using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace DiscordBots.BoredBot.Dice;

public static partial class InputParser
{
    private static readonly HashSet<int> AllowedDiceSides =
    [
        Dice.Four,
        Dice.Six,
        Dice.Ten,
        Dice.Twelve,
        Dice.Twenty,
        Dice.Hundred,
    ];

    private const int MaxDiceGroups = 30;
    private const int MaxTotalDice = 200;
    private const int MaxModifierMagnitude = 10_000;

    public static bool Parse(
        string userTextInput,
        [NotNullWhen(true)] out DiceParseResult? diceParseResult,
        out string? errorMessageToUser
    )
    {
        diceParseResult = null;
        errorMessageToUser = null;

        if (string.IsNullOrWhiteSpace(userTextInput))
        {
            errorMessageToUser = "Input must be a non-empty string.";
            return false;
        }

        var clean = userTextInput.ToLowerInvariant().Trim();
        clean = StripNoise(clean);

        if (RegexNumber().IsMatch(clean))
        {
            if (!int.TryParse(clean, out var sides))
            {
                errorMessageToUser = $"Unable to parse number '{clean}'.";
                return false;
            }
            if (!AllowedDiceSides.Contains(sides))
            {
                errorMessageToUser =
                    $"Allowed dice sides are {string.Join(", ", AllowedDiceSides)}. Received: {sides}";
                return false;
            }
            diceParseResult = new DiceParseResult { Dices = [sides], Modifier = 0 };
            return true;
        }

        var rawTokens = RegexToken().Matches(clean).Select(m => m.Value).ToArray();
        
        switch (rawTokens.Length)
        {
            case 0:
                errorMessageToUser =
                    $"Unable to parse any dice or modifiers. Examples: '3d6+5', 'd20-2', '2d8+1d6+4'. This is what you wrote: '{userTextInput}'.";
                return false;
            case > MaxDiceGroups + 50:
                errorMessageToUser = "Expression too long / complex.";
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
                    errorMessageToUser = $"Too many dice groups (>{MaxDiceGroups}).";
                    return false;
                }

                var parts = tok.Split('d');
                if (parts.Length != 2)
                {
                    errorMessageToUser = $"Malformed dice token '{tok}'.";
                    return false;
                }

                var countPart = parts[0];
                var sidesPart = parts[1];

                int diceCount = 1;
                if (!string.IsNullOrEmpty(countPart))
                {
                    if (!int.TryParse(countPart, out diceCount) || diceCount <= 0)
                    {
                        errorMessageToUser = $"Invalid dice count in '{tok}'.";
                        return false;
                    }
                }

                if (!int.TryParse(sidesPart, out var sides) || sides <= 0)
                {
                    errorMessageToUser = $"Invalid dice sides in '{tok}'.";
                    return false;
                }

                if (!AllowedDiceSides.Contains(sides))
                {
                    errorMessageToUser =
                        $"Allowed dice sides are {string.Join(", ", AllowedDiceSides)}. Received: {sides}";
                    return false;
                }

                if (diceList.Count + diceCount > MaxTotalDice)
                {
                    errorMessageToUser = $"Too many dice requested (>{MaxTotalDice} total).";
                    return false;
                }

                for (int i = 0; i < diceCount; i++)
                    diceList.Add(sides);
            }
            else
            {
                if (!int.TryParse(tok, out var value))
                {
                    errorMessageToUser = $"Invalid modifier '{tok}'.";
                    return false;
                }
                if (Math.Abs(value) > MaxModifierMagnitude)
                {
                    errorMessageToUser =
                        $"Modifier magnitude too large (> {MaxModifierMagnitude}).";
                    return false;
                }
                modifierTotal += value;
            }
        }

        diceParseResult = new DiceParseResult { Dices = diceList, Modifier = modifierTotal };
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
