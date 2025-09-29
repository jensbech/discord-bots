using System.Text.RegularExpressions;

namespace DiscordBots.BoredBot.Commands;

internal static partial class PreviewCleaner
{
    public static string Clean(string? preview)
    {
        if (string.IsNullOrWhiteSpace(preview))
            return string.Empty;
        var cleaned = preview
            .Replace("<strong>", "**", StringComparison.OrdinalIgnoreCase)
            .Replace("</strong>", "**", StringComparison.OrdinalIgnoreCase)
            .Replace("<u>", "__", StringComparison.OrdinalIgnoreCase)
            .Replace("</u>", "__", StringComparison.OrdinalIgnoreCase);

        cleaned = Regex.Replace(cleaned, "<img[^>]*>", string.Empty, RegexOptions.IgnoreCase);
        cleaned = StripTags().Replace(cleaned, string.Empty);
        cleaned = CollapseBrTags().Replace(cleaned, "\n");
        cleaned = CollapseWhitespaceNewline().Replace(cleaned, "\n");
        return cleaned.Trim();
    }

    [GeneratedRegex("\\n{2,}")]
    private static partial Regex CollapseBrTags();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex StripTags();

    [GeneratedRegex("\\s+\\n")]
    private static partial Regex CollapseWhitespaceNewline();
}
