namespace DiscordBots.DiceRoller
{
    public class DiceRoller
    {
        private readonly string _username;

        public DiceRoller(string username)
        {
            _username = username;
        }

        public async Task<(int RollResult, string? Message)> Roll(int dice)
        {
            var allowedDice = new[] { Dice.Four, Dice.Six, Dice.Ten, Dice.Twelve, Dice.Twenty, Dice.Hundred };
            if (!allowedDice.Contains(dice))
            {
                throw new ArgumentException($"Invalid dice type: {dice}");
            }

            var (outcome, crit) = GetSingleDiceRollOutcome(dice);

            if (crit.Failure || crit.Success)
            {
                return (outcome, crit.Success 
                    ? await HandleCritical(Critical.Success)
                    : await HandleCritical(Critical.Fail));
            }

            return (outcome, null);
        }

        private (int Outcome, (bool Failure, bool Success) Crit) GetSingleDiceRollOutcome(int dice)
        {
            var random = new Random();
            var roll = random.Next(1, dice + 1);

            return dice switch
            {
                Dice.Twenty => (roll, (roll == 1, roll == 20)),
                _ => (roll, (false, false))
            };
        }

        private async Task<string> HandleCritical(Critical critical)
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