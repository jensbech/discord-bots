import aiohttp
from urllib.parse import urlencode
from discord import Embed
import discord
import re


async def search(interaction: discord.Interaction, baseurl: str, auth_header: dict,
                 query: str, page: int = 1, count: int = 10):
    query_params = {
        'query': query,
        'page': page,
        'count': count
    }

    encoded_query = urlencode(query_params)
    search_url = f"{baseurl}/search?{encoded_query}"
    async with aiohttp.ClientSession() as session:
        async with session.get(search_url, headers=auth_header) as response:
            if response.status == 200:
                data = await response.json()

                n_results = data['total']
                results_message = f"Showing {min(len(data['data']), count)} of {n_results} wiki results. " \
                    f"Consider a more specific search!" if n_results > count else ""
                if n_results > 0:
                    embeds = []
                    for result in data['data'][:count]:
                        preview_content = result['preview_html']['content']
                        preview_content = preview_content.replace(
                            '<strong>', '**').replace('</strong>', '**')
                        preview_content = preview_content.replace(
                            '<u>', '__').replace('</u>', '__')
                        preview_content = re.sub(
                            '<img[^>]*>', '', preview_content)
                        preview_content = re.sub(
                            '\n{2,}', '\n', preview_content)
                        preview_content = re.sub(
                            '\s*\n\s*\n\s*', '\n', preview_content)

                        embed = Embed(
                            title=result['name'],
                            url=result['url'],
                            color=0x008080
                        )
                        if preview_content:
                            embed.add_field(
                                name="", value=preview_content, inline=False)
                        embeds.append(embed)

                    await interaction.response.send_message(embeds=embeds, content=results_message)
                else:
                    await interaction.response.send_message("No results found.")
            else:
                await interaction.response.send_message("Failed to fetch results.")
