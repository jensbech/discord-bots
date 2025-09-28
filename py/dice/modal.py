import discord
from discord.ui import Modal, TextInput
from commands import roll


class DiceRollModal(Modal):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)

        self.add_item(
            TextInput(label="Dice", placeholder="e.g., 1d6", required=True))
        self.add_item(TextInput(label="Modifier Type",
                      placeholder="+ or -", required=False))
        self.add_item(TextInput(label="Modifier Value",
                      placeholder="e.g., 2", required=False))

    async def callback(self, interaction: discord.Interaction):
        dice = self.children[0].value
        modifier_type = self.children[1].value
        modifier_value = self.children[2].value
        modifier = 0

        if modifier_type and modifier_value:
            try:
                modifier = int(modifier_value)
                if modifier_type == '-':
                    modifier *= -1
            except ValueError:
                await interaction.response.send_message('Invalid modifier value!')
                return

        await roll(interaction, dice, modifier)
