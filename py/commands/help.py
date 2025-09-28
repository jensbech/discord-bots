import discord


async def help(interaction: discord.Interaction):
    messages = [
        "Available commands:",
        "`/search sasha yarna` - Search the Bored Gods Wiki",
        "`/roll 2d6+3` - Roll some dice",
        "`/weather` - Check the Stone-upon-hill weather!",
        "`/chat what is disengage` - Ask about DND rules",
    ]
    help_message = "\n".join(messages)
    await interaction.response.send_message(help_message)
