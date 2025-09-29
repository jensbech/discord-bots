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
        }

        public async Task<BookStackSearchResponse?> SearchAsync(
            string query,
            int page = 1,
            int count = 100,
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

                req.Headers.Clear();
                var tokenValue = $"{_options.ApiId}:{_options.ApiKey}";
                req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Token",
                    tokenValue
                );

                var resp = await _http.SendAsync(req, ct);

                if (!resp.IsSuccessStatusCode)
                {
                    string? errorBody = null;
                    try
                    {
                        errorBody = await resp.Content.ReadAsStringAsync(ct);
                    }
                    catch { }
                    var snippet = errorBody is null
                        ? "<empty>"
                        : errorBody.Substring(0, Math.Min(160, errorBody.Length));
                    if (resp.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        _logger.LogError(
                            "BookStack API returned 403 Forbidden for query '{Query}'. This indicates the API token is invalid, expired, or the user lacks 'Access System API' permission. Check the BookStack user settings. Body: {Snippet}",
                            query,
                            snippet
                        );
                    }
                    else if (resp.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        _logger.LogWarning(
                            "BookStack API rate limit exceeded (429) for query '{Query}'. Default limit is 180 requests/minute. Body: {Snippet}",
                            query,
                            snippet
                        );
                    }
                    else
                    {
                        _logger.LogWarning(
                            "BookStack search failed with {Status} for query '{Query}'. Body: {Snippet}",
                            resp.StatusCode,
                            query,
                            snippet
                        );
                    }
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

        public async Task<string?> GetPageHtmlAsync(string pageUrl, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(pageUrl))
                return null;

            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, pageUrl);

                req.Headers.Clear();
                var tokenValue = $"{_options.ApiId}:{_options.ApiKey}";
                req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Token",
                    tokenValue
                );

                _logger.LogDebug(
                    "BookStack page fetch: {Method} {Url} with ApiId: {ApiIdPrefix}, Full Token Length: {TokenLength}",
                    req.Method,
                    req.RequestUri,
                    _options.ApiId[..Math.Min(10, _options.ApiId.Length)],
                    tokenValue.Length
                );

                var resp = await _http.SendAsync(req, ct);
                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Failed to fetch BookStack page {Url} status {Status}",
                        pageUrl,
                        resp.StatusCode
                    );
                    return null;
                }
                var html = await resp.Content.ReadAsStringAsync(ct);
                if (string.IsNullOrWhiteSpace(html))
                    return null;
                return html;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching BookStack page {Url}", pageUrl);
                return null;
            }
        }

        public async Task<string?> GetPageTextAsync(string pageUrl, CancellationToken ct = default)
        {
            var html = await GetPageHtmlAsync(pageUrl, ct);
            if (html is null)
                return null;

            try
            {
                var cleaned = System.Text.RegularExpressions.Regex.Replace(
                    html,
                    "<script[\\s\\S]*?</script>",
                    string.Empty,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );
                cleaned = System.Text.RegularExpressions.Regex.Replace(
                    cleaned,
                    "<style[\\s\\S]*?</style>",
                    string.Empty,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );
                cleaned = System.Text.RegularExpressions.Regex.Replace(
                    cleaned,
                    "<head[\\s\\S]*?</head>",
                    string.Empty,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );
                cleaned = System.Text.RegularExpressions.Regex.Replace(
                    cleaned,
                    "<footer[\\s\\S]*?</footer>",
                    string.Empty,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );
                cleaned = System.Text.RegularExpressions.Regex.Replace(
                    cleaned,
                    "<nav[\\s\\S]*?</nav>",
                    string.Empty,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );
                cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, "<[^>]+>", " ");
                cleaned = System.Web.HttpUtility.HtmlDecode(cleaned);
                cleaned = System.Text.RegularExpressions.Regex.Replace(
                    cleaned,
                    "&[a-zA-Z0-9#]+;",
                    " "
                );
                cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, "\n{2,}", "\n");
                cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, "\\s{2,}", " ");
                return cleaned.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning BookStack page {Url}", pageUrl);
                return null;
            }
        }
    }
}
