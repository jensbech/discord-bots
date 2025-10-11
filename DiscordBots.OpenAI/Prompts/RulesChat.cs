namespace DiscordBots.OpenAI.Prompts;

public static class RulesChat
{
    public static string GetSystemPrompt()
    {
        return "You are a chatbot replying ONLY to questions about Dungeons and Dragons 5E rules. You refuse to discuss anything else but DND rules.";
    }
}