using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;
using DiscordBots.BookStack.Interfaces;
using DiscordBots.BookStack.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiscordBots.BookStack;

internal sealed class BookStackClient(
    HttpClient http,
    IOptions<BookStackOptions> options,
    ILogger<BookStackClient> logger
) : IBookStackClient
{
    private readonly BookStackOptions _options = options.Value;

    public async Task<BookStackSearchResponse?> SearchAsync(
        string query,
        int page = 1,
        int count = 100
    )
    {
        var queryString = $"query={Uri.EscapeDataString(query)}&page={page}&count={count}";
        using var req = new HttpRequestMessage(HttpMethod.Get, $"search?{queryString}");
        req.Headers.Authorization = new AuthenticationHeaderValue(
            "Token",
            $"{_options.ApiId}:{_options.ApiKey}"
        );

        using var res = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);

        var json = await res.Content.ReadAsStringAsync();
        try
        {
            return JsonSerializer.Deserialize<BookStackSearchResponse>(json);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to deserialize BookStack search response");
            return null;
        }
    }

    public async Task<string?> GetEntityHtmlAsync(string type, int id)
    {
        if (id <= 0 || string.IsNullOrWhiteSpace(type))
            return null;
        var endpoint = MapTypeToEndpoint(type);
        using var req = new HttpRequestMessage(HttpMethod.Get, $"{endpoint}/{id}");
        
        req.Headers.Authorization = new AuthenticationHeaderValue(
            "Token",
            $"{_options.ApiId}:{_options.ApiKey}"
        );
        req.Headers.Accept.Clear();
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        
        using var res = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
     
        var body = await res.Content.ReadAsStringAsync();
        
        // Log response details for debugging
        logger.LogInformation("BookStack API Response: Status={Status}, Body length={Length}", 
            res.StatusCode, body?.Length ?? 0);
        
        if (!res.IsSuccessStatusCode)
        {
            logger.LogError("BookStack API error for {Type}/{Id}: {Status} - {Body}", 
                type, id, res.StatusCode, body);
            return null;
        }
        
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            
            if (root.TryGetProperty("html", out var htmlProp))
                return htmlProp.GetString();
            if (root.TryGetProperty("content", out var contentProp))
                return contentProp.GetString();
            
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to parse entity HTML for Type={Type} Id={Id}. Response body: {Body}", 
                type, id, body);
            return null;
        }
    }

    private static string MapTypeToEndpoint(string type) =>
        type.ToLowerInvariant() switch
        {
            "page" => "pages",
            "chapter" => "chapters",
            "book" => "books",
            "bookshelf" => "bookshelves",
            _ => type + "s",
        };

  
}
