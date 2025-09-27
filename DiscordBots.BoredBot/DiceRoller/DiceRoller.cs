namespace DiscordBots.BoredBot.DiceRoller
{
    public class DiceRoller(string username)
    {
        private readonly string _username = username;

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
    }
}
