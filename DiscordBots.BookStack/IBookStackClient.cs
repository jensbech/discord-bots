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
    }
}
