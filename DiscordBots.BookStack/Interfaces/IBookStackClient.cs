using DiscordBots.BookStack.Models;

namespace DiscordBots.BookStack.Interfaces;

public interface IBookStackClient
{
    Task<BookStackSearchResponse?> SearchAsync(string query, int page = 1, int count = 20);
    Task<string?> GetEntityHtmlAsync(string type, int id);
}
