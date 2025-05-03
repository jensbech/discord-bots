// import { Elysia } from "elysia";
import { BoredBot } from "./boredBot";
import { boredBotCommands } from "./boredBot/commands";

// new Elysia().get("/", () => "Hello Elysia").listen(3000);
if (!process.env.DISCORD_BOT_TOKEN || !process.env.APPLICATION_ID) {
	throw new Error("DISCORD_BOT_TOKEN and APPLICATION_ID must be defined in the environment variables.");
}

await BoredBot.getInstance(process.env.DISCORD_BOT_TOKEN, process.env.APPLICATION_ID, boredBotCommands);
