import { SlashCommandBuilder } from "discord.js";
import { Command } from "../discordBot/types";

export const boredBotCommands = [
	new SlashCommandBuilder()
		.setName(Command.Roll)
		.setDescription("Roll dice (e.g. 'd20', '6d12-4', '2d8 + 1d6+4')")
		.addStringOption((option) =>
			option
				.setName("input")
				.setDescription("The dice you want to roll")
				.setRequired(true),
		),
	new SlashCommandBuilder()
		.setName(Command.Help)
		.setDescription("Displays a list of available commands."),
];
