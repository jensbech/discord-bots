using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;
using DiscordBots.BookStack.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiscordBots.BookStack
{
    public interface IBookStackClient
    {
        Task<BookStackSearchResponse?> SearchAsync(string query, int page = 1, int count = 10);
        Task<string?> GetPageHtmlAsync(string pageUrl);
    }

    internal sealed class BookStackClient(
        HttpClient http,
        IOptions<BookStackOptions> options,
        ILogger<BookStackClient> logger
    ) : IBookStackClient
    {
        private readonly HttpClient _http = http;
        private readonly BookStackOptions _options = options.Value;
        private readonly ILogger<BookStackClient> _logger = logger;

        public async Task<BookStackSearchResponse?> SearchAsync(
            string query,
            int page = 1,
            int count = 100
        )
        {
            var q = HttpUtility.ParseQueryString(string.Empty);
            q["query"] = query;
            q["page"] = page.ToString();
            q["count"] = count.ToString();
            var url = $"search?{q}";
            _logger.LogDebug("Making request to: {Url}", url);

            using var req = new HttpRequestMessage(HttpMethod.Get, url);

            req.Headers.Authorization = new AuthenticationHeaderValue(
                "Token",
                $"{_options.ApiId}:{_options.ApiKey}"
            );

            var res = await _http.SendAsync(req);

            if (!res.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to query bookstack: ${res.StatusCode}");
                return null;
            }

            return JsonSerializer.Deserialize<BookStackSearchResponse>(
                    await res.Content.ReadAsStringAsync()
                ) ?? null;
        }

        public async Task<string?> GetPageHtmlAsync(string pageUrl)
        {
            if (string.IsNullOrWhiteSpace(pageUrl))
                return null;

            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, pageUrl);

                var tokenValue = $"{_options.ApiId}:{_options.ApiKey}";
                req.Headers.Authorization = new AuthenticationHeaderValue("Token", tokenValue);

                var resp = await _http.SendAsync(req);
                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Failed to fetch BookStack page {Url} status {Status}",
                        pageUrl,
                        resp.StatusCode
                    );
                    return null;
                }

                return await resp.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching BookStack page {Url}", pageUrl);
                return null;
            }
        }
    }
}
