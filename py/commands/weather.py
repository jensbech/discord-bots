import discord
import random
import json


async def weather(interaction: discord.Interaction):
    with open("resources/forecast.json", "r") as forecasts_file:
        forecasts = json.load(forecasts_file)
        random_forecast = random.choice(list(forecasts.values()))
    await interaction.response.send_message(random_forecast)
