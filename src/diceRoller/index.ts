import { Critical, Dice } from "./types";

export class DiceRoller {
	private username: string;
	constructor(user: string) {
		this.username = user;
	}

	public async roll(
		dice: (typeof Dice)[keyof typeof Dice],
	): Promise<{ rollResult: number; message?: string }> {
		if (!Object.values(Dice).includes(dice))
			throw new Error(`Invalid dice type: ${dice}`);

		const { outcome: result, crit } = this.getSingleDiceRollOutcome(dice);

		if (crit.failure || crit.success) {
			return {
				rollResult: result,
				message: crit.success
					? await this.handleCritical(Critical.Success)
					: await this.handleCritical(Critical.Fail),
			};
		}
		return { rollResult: result };
	}

	private getSingleDiceRollOutcome(dice: (typeof Dice)[keyof typeof Dice]): {
		outcome: number;
		crit: { failure: boolean; success: boolean };
	} {
		const roll = Math.floor(Math.random() * dice) + 1;

		switch (dice) {
			case Dice.Twenty:
				return {
					outcome: roll,
					crit: { failure: roll === 1, success: roll === 20 },
				};
			default:
				return { outcome: roll, crit: { failure: false, success: false } };
		}
	}

	private async handleCritical(critical: Critical): Promise<string> {
		switch (critical) {
			case Critical.Fail:
				return "Critical FAIL!";
			case Critical.Success:
				return "Critical SUCCESS!";
		}
	}
}
