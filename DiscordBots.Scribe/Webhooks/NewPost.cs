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

            string summary;

            if (string.Equals(webhookData.EventType, "page_create", StringComparison.OrdinalIgnoreCase))
            {
                // For new pages, generate two-sentence AI summary using inline html content when available.
                summary = await GenerateCreateSummary(openAiClient, webhookData.Title, webhookData.HtmlContent, webhookData.RevisionSummary, logger);

                logger.LogInformation("Generated AI summary for page create: {Summary}", summary);
            }
            else
            {
                // For updates, try to fetch content with fallbacks
                var pageContent = await FetchPageContentWithFallbacks(
                    bookStackClient,
                    webhookData.Url,
                    webhookData.Title,
                    webhookData.Slug,
                    webhookData.PageId,
                    logger);

                if (string.IsNullOrWhiteSpace(pageContent))
                {
                    logger.LogWarning("Could not fetch content for page {PageId}, using title-only summary", webhookData.PageId);
                    summary = $"Page '{webhookData.Title}' has been updated.";
                }
                else
                {
                    summary = await GenerateSummary(openAiClient, webhookData.Title, pageContent, logger);
                }
            }

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

            const ulong channelId = 1426887048161132564;

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

        var slug = root.GetProperty("related_item").GetProperty("slug").GetString()
            ?? throw new InvalidOperationException("related_item.slug is null");

        var pageId = root.GetProperty("related_item").GetProperty("id").GetInt32();
        // Attempt to read raw HTML/content from the webhook for create events
        string? htmlContent = null;
        if (root.GetProperty("related_item").TryGetProperty("html", out var htmlElement) &&
            htmlElement.ValueKind == JsonValueKind.String)
        {
            htmlContent = htmlElement.GetString();
        }

        var updatedAtString = root.GetProperty("related_item").GetProperty("updated_at").GetString()
            ?? throw new InvalidOperationException("related_item.updated_at is null");

        var updatedAt = DateTime.Parse(updatedAtString);

        var revisionNumber = 1;
        string? revisionSummary = null;

        if (root.GetProperty("related_item").TryGetProperty("current_revision", out var currentRevision))
        {
            if (currentRevision.TryGetProperty("revision_number", out var revisionNumberElement))
            {
                revisionNumber = revisionNumberElement.GetInt32();
            }
            if (currentRevision.TryGetProperty("summary", out var summaryElement) &&
                summaryElement.ValueKind == JsonValueKind.String)
            {
                revisionSummary = summaryElement.GetString();
            }
        }

        return new WebhookData(url, author, title, slug, pageId, updatedAt, revisionNumber, eventType, revisionSummary, htmlContent);
    }

    private static async Task<string?> FetchPageContentWithFallbacks(
        IBookStackClient bookStackClient,
        string pageUrl,
        string pageTitle,
        string pageSlug,
        int pageId,
        ILogger logger)
    {
        // Try 1: Search by title/slug/url
        logger.LogInformation("Attempting to fetch page content via search for: {Title}", pageTitle);
        var searchContent = await FetchPageContentViaSearch(bookStackClient, pageUrl, pageTitle, pageSlug, logger);
        if (!string.IsNullOrWhiteSpace(searchContent))
        {
            return searchContent;
        }

        // Try 2: Direct API call by ID
        logger.LogInformation("Search failed, attempting direct fetch for page {PageId}", pageId);
        var directContent = await FetchPageContentById(bookStackClient, pageId, logger);
        if (!string.IsNullOrWhiteSpace(directContent))
        {
            return directContent;
        }

        return null;
    }

    private static async Task<string?> FetchPageContentViaSearch(
        IBookStackClient bookStackClient,
        string pageUrl,
        string pageTitle,
        string pageSlug,
        ILogger logger)
    {
        try
        {
            var searchResults = await bookStackClient.SearchAsync(pageTitle, 1, 10);

            if (searchResults?.Data == null || !searchResults.Data.Any())
            {
                logger.LogWarning("No search results found for page title: {Title}", pageTitle);
                return null;
            }

            logger.LogInformation("Search returned {Total} total results, {DataCount} data items",
                searchResults.Total, searchResults.Data.Count);

            // Find match by URL, slug, or title
            var exactMatch = searchResults.Data.FirstOrDefault(r =>
                r.Type.Equals("page", StringComparison.OrdinalIgnoreCase) &&
                (string.Equals(r.Url, pageUrl, StringComparison.OrdinalIgnoreCase)
                 || (!string.IsNullOrWhiteSpace(pageSlug) && r.Url.EndsWith($"/page/{pageSlug}", StringComparison.OrdinalIgnoreCase))
                 || r.Name.Equals(pageTitle, StringComparison.OrdinalIgnoreCase)));

            if (exactMatch == null)
            {
                logger.LogWarning("No exact page match found in search results for: {Title}", pageTitle);
                return null;
            }

            logger.LogInformation("Found page via search: Type={Type}, Id={Id}", exactMatch.Type, exactMatch.Id);

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

    private static async Task<string?> FetchPageContentById(
        IBookStackClient bookStackClient,
        int pageId,
        ILogger logger)
    {
        try
        {
            var content = await bookStackClient.GetEntityHtmlAsync("page", pageId);
            if (!string.IsNullOrWhiteSpace(content))
            {
                logger.LogInformation("Successfully fetched page {PageId} via direct API", pageId);
                return content;
            }

            logger.LogWarning("Direct API returned empty content for page {PageId}", pageId);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Direct fetch failed for page {PageId}", pageId);
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
 
    private static async Task<string> GenerateCreateSummary(
        IOpenAiClient openAiClient,
        string title,
        string? htmlContent,
        string? revisionSummary,
        ILogger logger)
    {
        // Prefer a non-trivial revision summary if provided
        if (!string.IsNullOrWhiteSpace(revisionSummary) && revisionSummary!.Trim().Length > 15)
        {
            return revisionSummary!;
        }
 
        try
        {
            // Fallback if we have no HTML content at all
            var contentForModel = htmlContent;
            if (string.IsNullOrWhiteSpace(contentForModel))
            {
                return $"New page '{title}' has been created.";
            }
 
            const int maxContentLength = 3000;
            if (contentForModel!.Length > maxContentLength)
            {
                contentForModel = contentForModel.Substring(0, maxContentLength) + "...";
            }
 
            var prompt =
                $"You are a concise documentation assistant. Based on the HTML content of a newly created knowledge base page, " +
                $"produce exactly two sentences: (1) a clear descriptive title (you may refine the original) and (2) a succinct summary of the page's purpose or key points. " +
                $"Do not use markdown, quotes, or bullet points. Keep total length under 250 characters.\n\n" +
                $"Original Title: {title}\n\nHTML Content:\n{contentForModel}";
 
            var aiResponse = await openAiClient.AskChat(prompt, new List<string> { contentForModel });
 
            if (string.IsNullOrWhiteSpace(aiResponse))
            {
                return $"New page '{title}' has been created.";
            }
 
            // Sanitize formatting and length
            aiResponse = aiResponse.Replace("\n", " ").Replace("\r", " ").Trim();
            if (aiResponse.Length > 250)
            {
                aiResponse = aiResponse.Substring(0, 247) + "...";
            }
 
            return aiResponse;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate AI create summary for page {Title}", title);
            return $"New page '{title}' has been created.";
        }
    }
 
    private record WebhookData(
        string Url,
        string Author,
        string Title,
        string Slug,
        int PageId,
        DateTime UpdatedAt,
        int RevisionNumber,
        string EventType,
        string? RevisionSummary,
        string? HtmlContent
    );
}
