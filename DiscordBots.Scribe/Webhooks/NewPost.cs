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

            // For newly created pages, add retry logic with delay to handle timing issues
            var pageContent = await FetchPageContentWithRetry(bookStackClient, webhookData.PageId, webhookData.EventType, logger);
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

    private static async Task<string?> FetchPageContentWithRetry(
        IBookStackClient bookStackClient, 
        int pageId, 
        string eventType, 
        ILogger logger)
    {
        var maxRetries = eventType == "page_create" ? 3 : 1;
        var delayMs = 1000; // Start with 1 second delay
        
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var content = await bookStackClient.GetEntityHtmlAsync("page", pageId);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    if (attempt > 1)
                    {
                        logger.LogInformation("Successfully fetched page {PageId} on attempt {Attempt}", pageId, attempt);
                    }
                    return content;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Attempt {Attempt} failed to fetch page {PageId}", attempt, pageId);
            }
            
            if (attempt < maxRetries)
            {
                logger.LogInformation("Page {PageId} not ready yet, waiting {DelayMs}ms before retry {Attempt}/{MaxRetries}", 
                    pageId, delayMs, attempt + 1, maxRetries);
                await Task.Delay(delayMs);
                delayMs *= 2; // Exponential backoff
            }
        }
        
        return null;
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
