using System.Net.Http.Json;
using System.Web;
using DiscordBots.BookStack.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiscordBots.BookStack
{
    internal sealed class BookStackClient : IBookStackClient
    {
        private readonly HttpClient _http;
        private readonly BookStackOptions _options;
        private readonly ILogger<BookStackClient> _logger;

        public BookStackClient(
            HttpClient http,
            IOptions<BookStackOptions> options,
            ILogger<BookStackClient> logger
        )
        {
            _http = http;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<BookStackSearchResponse?> SearchAsync(
            string query,
            int page = 1,
            int count = 10,
            CancellationToken ct = default
        )
        {
            if (string.IsNullOrWhiteSpace(query))
                return null;
            var builder = new UriBuilder(new Uri(new Uri(_options.BaseUrl), "search"));
            var q = HttpUtility.ParseQueryString(string.Empty);
            q["query"] = query;
            q["page"] = page.ToString();
            q["count"] = count.ToString();
            builder.Query = q.ToString();
            var url = builder.Uri;

            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.Add("Authorization", $"Token {_options.ApiId}:{_options.ApiKey}");
                var resp = await _http.SendAsync(req, ct);
                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "BookStack search failed with {Status} for query '{Query}'",
                        resp.StatusCode,
                        query
                    );
                    return null;
                }
                var result = await resp.Content.ReadFromJsonAsync<BookStackSearchResponse>(
                    cancellationToken: ct
                );
                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing BookStack search for '{Query}'", query);
                return null;
            }
        }
    }
}
