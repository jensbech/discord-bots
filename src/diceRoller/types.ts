export const Dice = {
	Four: 4,
	Six: 6,
	Ten: 10,
	Twelve: 12,
	Twenty: 20,
	Hundrer: 100,
};

export enum Critical {
	Fail = "fail",
	Success = "success",
}

export type DiceParseResult = {
	dices: number[];
	mod: number;
};
