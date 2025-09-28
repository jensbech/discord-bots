import discord
import asyncio
from llama_index import VectorStoreIndex, SimpleDirectoryReader


async def chat(interaction: discord.Interaction, question: str):
    try:
        await interaction.response.defer()

        documents = SimpleDirectoryReader("resources").load_data()
        index = VectorStoreIndex.from_documents(documents)
        query_engine = index.as_query_engine()

        pre_prompt = "You are a chatbot replying ONLY to questions about Dungeons and Dragons 5E rules. \
            You refuse to discuss anything else but DND rules. \
                  Your question is: "
        full_question = pre_prompt + question

        response = query_engine.query(full_question)

        await asyncio.sleep(1)

        await interaction.followup.send(response)
    except Exception:
        await interaction.followup.send("Something went wrong, and I don't know what.")
