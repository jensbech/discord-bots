import discord
from discord import app_commands
import os

from dotenv import load_dotenv

load_dotenv()


class BookStackAPIClient(discord.Client):
    def __init__(self, *, intents: discord.Intents):
        super().__init__(intents=intents)
        self.tree = app_commands.CommandTree(self)
        self.api_id = os.getenv('BOOKSTACK_API_ID')
        self.api_key = os.getenv('BOOKSTACK_API_KEY')
        self.auth_header = {
            'Authorization': f'Token { self.api_id}:{ self.api_key}'}
        self.discord_token = os.getenv('DISCORD_TOKEN')

    async def setup_hook(self):
        guild_id = int(os.getenv('GUILD_ID'))
        guild_object = discord.Object(id=guild_id)
        self.tree.copy_global_to(guild=guild_object)
        await self.tree.sync(guild=guild_object)
