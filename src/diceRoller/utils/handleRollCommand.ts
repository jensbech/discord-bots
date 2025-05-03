import { DiceRoller } from "../discord/dice";
import { parseDiceUserInput } from "./parseDiceUserInput";
import type { DiceParseResult } from "../types";

export async function handleRollCommand(
	inputStringFromUser: string,
	username: string,
): Promise<string> {
	let parsedInputResult: DiceParseResult = { dices: [], mod: 0 };

	try {
		parsedInputResult = parseDiceUserInput(inputStringFromUser);
	} catch (error) {
		if (error instanceof Error) return error.message;
	}

	const roller = new DiceRoller(username);
	const resultsMessages: string[] = [];

	if (parsedInputResult.dices.length === 1) {
		const singleDie = parsedInputResult.dices[0];
		const { rollResult, message } = await roller.roll(singleDie);

		const mod = parsedInputResult.mod;
		const finalResult = rollResult + mod;

		let singleLine = `(${singleDie.toString()}) => ${rollResult}`;
		if (mod !== 0) {
			const sign = mod > 0 ? "+" : "-";
			singleLine += ` ${sign}${Math.abs(mod)} = ${finalResult}`;
		}

		if (message) {
			singleLine += `\n${message}`;
		}

		resultsMessages.push(singleLine);
	} else {
		resultsMessages.push(`You rolled ${parsedInputResult.dices.length} dice!`);

		let sum = 0;
		let rollCount = 1;

		for (const dieInput of parsedInputResult.dices) {
			const { rollResult, message } = await roller.roll(dieInput);
			sum += rollResult;

			const prefix = `Roll #${rollCount}: `;
			const suffix = message ? ` **${message}**` : "";

			resultsMessages.push(
				`${prefix}(d${dieInput.toString()}) => ${rollResult}${suffix}`,
			);
			rollCount++;
		}

		const mod = parsedInputResult.mod;
		const finalResult = sum + mod;
		if (mod !== 0) {
			const sign = mod > 0 ? "+" : "-";

			resultsMessages.push(
				`Result: ${sum} ${sign} ${Math.abs(mod)} = ${finalResult}`,
			);
		} else {
			resultsMessages.push(`**Final result: ${sum}**`);
		}
	}

	return resultsMessages.join("\n");
}
