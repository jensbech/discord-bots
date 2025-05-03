import {
	Client,
	GatewayIntentBits,
	REST,
	Routes,
	type SlashCommandOptionsOnlyBuilder,
} from "discord.js";

export abstract class DiscordBot {
	private token: string;
	private applicationId: string;
	protected client: Client;
	private commands: SlashCommandOptionsOnlyBuilder[];

	protected constructor(
		token: string,
		applicationId: string,
		commands: SlashCommandOptionsOnlyBuilder[],
	) {
		this.client = new Client({
			intents: [
				GatewayIntentBits.Guilds,
				GatewayIntentBits.GuildMessages,
				GatewayIntentBits.MessageContent,
			],
		});
		this.token = token;
		this.applicationId = applicationId;
		this.commands = commands;
	}

	private async login(): Promise<void> {
		if (!this.token) {
			throw new Error("No token provided when attempting to log in bot");
		}
		try {
			await this.client.login(this.token);
		} catch (error) {
			throw new Error(`Bot failed to log in: ${error}`);
		}
	}

	private async registerCommands(): Promise<void> {
		try {
			await new REST({ version: "10" })
				.setToken(this.token)
				.put(Routes.applicationCommands(this.applicationId), {
					body: this.commands,
				});
		} catch (error) {
			throw new Error(`Failed to register slash commands: ${error}`);
		}
	}

	protected async initialize(botName: string): Promise<void> {
		console.log(`Initializing bot '${botName}'...`);
		try {
			await this.login();
			await this.registerCommands();
			console.log(`${botName} is ready!`);
		} catch (error) {
			console.log("Initialization error:", error);
		}
	}
}
