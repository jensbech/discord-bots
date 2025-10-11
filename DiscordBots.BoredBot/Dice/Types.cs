namespace DiscordBots.BoredBot.Dice;

public static class Dice
{
    public const int Four = 4;
    public const int Six = 6;
    public const int Ten = 10;
    public const int Twelve = 12;
    public const int Twenty = 20;
    public const int Hundred = 100;
}

public enum Critical
{
    Fail,
    Success
}

public class DiceParseResult
{
    public List<int> Dices { get; init; } = [];
    public int Modifier { get; init; }
}