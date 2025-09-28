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
        private static readonly System.Text.Json.JsonSerializerOptions s_jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        public BookStackClient(
            HttpClient http,
            IOptions<BookStackOptions> options,
            ILogger<BookStackClient> logger
        )
        {
            _http = http;
            _options = options.Value;
            _logger = logger;

            _logger.LogDebug(
                "BookStackClient initialized with BaseUrl: {BaseUrl}, HttpClient BaseAddress: {BaseAddress}",
                string.IsNullOrWhiteSpace(_options.BaseUrl) ? "<empty>" : _options.BaseUrl,
                _http.BaseAddress
            );
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

            var q = HttpUtility.ParseQueryString(string.Empty);
            q["query"] = query;
            q["page"] = page.ToString();
            q["count"] = count.ToString();
            var url = $"search?{q}";

            _logger.LogDebug("Making request to: {Url}", url);

            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.Add("Authorization", $"Token {_options.ApiId}:{_options.ApiKey}");
                var resp = await _http.SendAsync(req, ct);

                _logger.LogDebug(
                    "Response status: {Status}, RequestUri: {Uri}",
                    resp.StatusCode,
                    resp.RequestMessage?.RequestUri
                );

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "BookStack search failed with {Status} for query '{Query}'",
                        resp.StatusCode,
                        query
                    );
                    return null;
                }

                var responseContent = await resp.Content.ReadAsStringAsync(ct);
                _logger.LogDebug(
                    "BookStack API Response (first 200 chars): {Content}",
                    responseContent[..Math.Min(200, responseContent.Length)]
                );

                if (string.IsNullOrWhiteSpace(responseContent))
                {
                    _logger.LogWarning(
                        "BookStack API returned empty response for query '{Query}'",
                        query
                    );
                    return null;
                }

                if (responseContent[0] == '<')
                {
                    _logger.LogWarning(
                        "BookStack API did not return JSON for query '{Query}'. Likely incorrect base URL or authentication; first 100 chars: {Snippet}",
                        query,
                        responseContent.Substring(0, Math.Min(100, responseContent.Length))
                    );
                    return null;
                }

                var result = System.Text.Json.JsonSerializer.Deserialize<BookStackSearchResponse>(
                    responseContent,
                    s_jsonOptions
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
