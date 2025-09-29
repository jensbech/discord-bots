using DiscordBots.BookStack.Models;

namespace DiscordBots.BookStack
{
    public interface IBookStackClient
    {
        Task<BookStackSearchResponse?> SearchAsync(
            string query,
            int page = 1,
            int count = 10,
            CancellationToken ct = default
        );

        Task<string?> GetPageHtmlAsync(string pageUrl, CancellationToken ct = default);

        Task<string?> GetPageTextAsync(string pageUrl, CancellationToken ct = default);
    }
}
