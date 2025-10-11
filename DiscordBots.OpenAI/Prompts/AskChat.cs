namespace DiscordBots.OpenAI.Prompts;

public static class AskChat
{
    public static (string system, string question) Get(string query, string[] documents)
    {
        const string system =
            "Write a comprehensive answer to the user's question based on the provided context."
            + "If the context provides multiple answers, combine them into a single answer."
            + "If there is no relevant information in the documents, say 'I don't know'. Do not make up an answer."
            + "If any retrieved article in the documents below does not relate to the question, omit it from your answer."
            + "Do NOT provide sources or URLs in your answer."
            + "Your tone is that of a story teller, but your job is to reproduce the source material in a factual way. You may assume the reader is already familiar with the world setting";

        var question =
            $"User Question: {query}\n\n Context for answering your query: {string.Join(
                "\n\n---\n\n",
                documents.Select((d, i) => $"Document {i + 1}:\n{d}")
            )}";

        return (system, question);
    }
}
