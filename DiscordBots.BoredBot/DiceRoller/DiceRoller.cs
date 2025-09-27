namespace DiscordBots.BoredBot.DiceRoller
{
    public class DiceRoller(string username)
    {
        private readonly string _username = username;

        public static async Task<(int RollResult, string? Message)> Roll(int sentDice)
        {
            var allowedDice = new[] { Dice.Four, Dice.Six, Dice.Ten, Dice.Twelve, Dice.Twenty, Dice.Hundred };

            if (!allowedDice.Contains(sentDice))
                throw new ArgumentException($"Invalid dice type: {sentDice}");

            var (outcome, crit) = GetSingleDiceRollOutcome(sentDice);

            if (crit.Failure || crit.Success)
            {
                return (outcome, crit.Success
                    ? await HandleCritical(Critical.Success)
                    : await HandleCritical(Critical.Fail));
            }

            return (outcome, null);
        }

        private static (int Outcome, (bool Failure, bool Success) Crit) GetSingleDiceRollOutcome(int dice)
        {
            var random = new Random();
            var roll = random.Next(1, dice + 1);

            return dice switch
            {
                Dice.Twenty => (roll, (roll == 1, roll == 20)),
                _ => (roll, (false, false))
            };
        }

        private static async Task<string> HandleCritical(Critical critical)
        {
            await Task.Delay(0);
            return critical switch
            {
                Critical.Fail => "Critical FAIL!",
                Critical.Success => "Critical SUCCESS!",
                _ => string.Empty
            };
        }
    }
}