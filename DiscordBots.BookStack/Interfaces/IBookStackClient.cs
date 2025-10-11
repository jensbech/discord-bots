using DiscordBots.BookStack.Models;

namespace DiscordBots.BookStack.Interfaces;

public interface IBookStackClient
{
    Task<BookStackSearchResponse?> SearchAsync(string query, int page = 1, int count = 10);
    Task<string?> GetPageHtmlAsync(string pageUrl);
}
