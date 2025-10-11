namespace DiscordBots.OpenAI.Prompts;

public static class AskChat
{
    public static (string system, string question) Get(string query, string[] documents)
    {
        const string system =
            "Write a comprehensive, well-structured answer (multiple paragraphs) summarizing and synthesizing the information. Write ALL that is required, without restraint"
            + "Assume the reader is familiar with the setting, no need for fluff about that."
            + "If any retrieved article does not relate to the question, omit it from your answer."
            + "Your tone is that of a story teller, but your job is to reproduce the source material in a factual way. You may assume the reader is already familiar with the world setting";

        var question =
            $"User Question: {query}\n\n Context for answering your query: {string.Join(
                "\n\n---\n\n",
                documents.Select((d, i) => $"Document {i + 1}:\n{d}")
            )}";

        return (system, question);
    }
}