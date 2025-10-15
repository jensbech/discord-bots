using System.Text.Json;
using Discord;
using DiscordBots.BookStack;
using DiscordBots.BookStack.Interfaces;
using DiscordBots.OpenAI.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiscordBots.Scribe.Webhooks;

public class NewPost
{
    public static async Task<IResult> SendAsync(
        HttpRequest request,
        ScribeBotBase scribeBotBase,
        ILogger<NewPost> logger,
        IOptions<BookStackOptions> bookStackOptions,
        IBookStackClient bookStackClient,
        IOpenAiClient openAiClient
    )
    {
        try
        {
            var webhookData = await ParseWebhookData(request);

            logger.LogInformation("New post detected: {Title} by {Author} at {Url}",
                webhookData.Title, webhookData.Author, webhookData.Url);

            logger.LogInformation("Webhook data: PageId={PageId}, EventType={EventType}",
                webhookData.PageId, webhookData.EventType);

            // Use search API to find the page (same approach as Ask command)
            var pageContent = await FetchPageContentViaSearch(bookStackClient, webhookData.Title, logger);
            if (string.IsNullOrWhiteSpace(pageContent))
            {
                logger.LogWarning("Could not fetch content for page {PageId}", webhookData.PageId);
                pageContent = "Content could not be retrieved.";
            }

            // Generate AI summary
            var summary = await GenerateSummary(openAiClient, webhookData.Title, pageContent, logger);



            // Create embed
            var footerText = webhookData.EventType == "page_create"
                ? "Page created"
                : $"Page updated • Revision #{webhookData.RevisionNumber}";

            var embed = new EmbedBuilder()
                .WithTitle(webhookData.Title)
                .WithAuthor(webhookData.Author)
                .WithDescription(summary)
                .WithUrl(webhookData.Url)
                .WithColor(Color.Blue)
                .WithTimestamp(webhookData.UpdatedAt)
                .WithFooter(footerText)
                .Build();

            // Send to Discord channel
            const ulong channelId = 123456789012345678; // TODO: Replace with actual channel ID

            var botClient = scribeBotBase.DiscordClient;
            if (botClient.GetChannel(channelId) is ITextChannel channel)
            {
                await channel.SendMessageAsync(embed: embed);
                logger.LogInformation("Successfully sent webhook notification to channel {ChannelId}", channelId);
            }
            else
            {
                logger.LogError("Could not find or access channel {ChannelId}", channelId);
                return Results.StatusCode(500);
            }

            return Results.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled error processing NewPost webhook");
            return Results.StatusCode(500);
        }
    }

    private static async Task<WebhookData> ParseWebhookData(HttpRequest request)
    {
        var body = await JsonDocument.ParseAsync(request.Body);
        var root = body.RootElement;

        var eventType = root.GetProperty("event").GetString()
            ?? throw new InvalidOperationException("event is null");

        var url = root.GetProperty("url").GetString()
            ?? throw new InvalidOperationException("url is null");

        var author = root.GetProperty("triggered_by").GetProperty("name").GetString()
            ?? throw new InvalidOperationException("triggered_by.name is null");

        var title = root.GetProperty("related_item").GetProperty("name").GetString()
            ?? throw new InvalidOperationException("related_item.name is null");

        var pageId = root.GetProperty("related_item").GetProperty("id").GetInt32();

        var updatedAtString = root.GetProperty("related_item").GetProperty("updated_at").GetString()
            ?? throw new InvalidOperationException("related_item.updated_at is null");

        var updatedAt = DateTime.Parse(updatedAtString);

        var revisionNumber = 1;
        if (root.GetProperty("related_item").TryGetProperty("current_revision", out var currentRevision))
        {
            if (currentRevision.TryGetProperty("revision_number", out var revisionNumberElement))
            {
                revisionNumber = revisionNumberElement.GetInt32();
            }
        }

        return new WebhookData(url, author, title, pageId, updatedAt, revisionNumber, eventType);
    }

    private static async Task<string?> FetchPageContentViaSearch(
        IBookStackClient bookStackClient, 
        string pageTitle, 
        ILogger logger)
    {
        try
        {
            logger.LogInformation("Searching for page with title: {Title}", pageTitle);
            
            // Use search API to find the page (we know this works from Ask command)
            var searchResults = await bookStackClient.SearchAsync(pageTitle, 1, 10);
            if (searchResults?.Data == null || !searchResults.Data.Any())
            {
                logger.LogWarning("No search results found for page title: {Title}", pageTitle);
                return null;
            }
            
            // Find exact title match
            var exactMatch = searchResults.Data.FirstOrDefault(r => 
                r.Name.Equals(pageTitle, StringComparison.OrdinalIgnoreCase) && 
                r.Type.Equals("page", StringComparison.OrdinalIgnoreCase));
                
            if (exactMatch == null)
            {
                logger.LogWarning("No exact page match found for title: {Title}", pageTitle);
                return null;
            }
            
            logger.LogInformation("Found page via search: Type={Type}, Id={Id}", exactMatch.Type, exactMatch.Id);
            
            // Now use the search result to get the content (same as Ask command does)
            var content = await bookStackClient.GetEntityHtmlAsync(exactMatch.Type, exactMatch.Id);
            
            if (!string.IsNullOrWhiteSpace(content))
            {
                logger.LogInformation("Successfully fetched page content via search for: {Title}", pageTitle);
                return content;
            }
            
            logger.LogWarning("Page found via search but content was empty for: {Title}", pageTitle);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch page content via search for title: {Title}", pageTitle);
            return null;
        }
    }

    private static async Task<string> GenerateSummary(
        IOpenAiClient openAiClient,
        string title,
        string content,
        ILogger logger)
    {
        try
        {
            const int maxContentLength = 2000;
            var truncatedContent = content.Length > maxContentLength
                ? content.Substring(0, maxContentLength) + "..."
                : content;

            var prompt = $"Create a single sentence summary of this page. " +
                        $"Title: {title}\n\nContent: {truncatedContent}";

            var summary = await openAiClient.AskChat(prompt, new List<string> { truncatedContent });

            if (string.IsNullOrWhiteSpace(summary))
            {
                return $"Page '{title}' has been updated.";
            }

            return summary.Length > 200 ? summary.Substring(0, 197) + "..." : summary;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate AI summary for page {Title}", title);
            return $"Page '{title}' has been updated.";
        }
    }

    private record WebhookData(
        string Url,
        string Author,
        string Title,
        int PageId,
        DateTime UpdatedAt,
        int RevisionNumber,
        string EventType
    );
}
