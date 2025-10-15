using Discord;
using Discord.WebSocket;
using DiscordBots.OpenAI.Interfaces;
using DiscordBots.Scribe.Commands.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;

namespace DiscordBots.Scribe.Commands.Handlers;

internal sealed class Tldr(IOpenAiClient openAi) : ISlashCommandHandler
{
    public string Name => "tldr";

    public async Task HandleAsync(SocketSlashCommand command, ILogger logger)
    {
        await command.DeferAsync();

        try
        {
            var channel = command.Channel;
            if (channel is not ITextChannel textChannel)
            {
                await command.FollowupAsync("This command can only be used in text channels.", ephemeral: true);
                logger.LogInformation("/tldr invoked in non-text channel");
                return;
            }

            var messages = new List<IMessage>();
            IMessage? lastUserMessage = null;
            
            var lastMessageId = (ulong?)null;
            const int searchLimit = 500;
            var messagesChecked = 0;
            
            while (messagesChecked < searchLimit)
            {
                var batch = lastMessageId.HasValue 
                    ? await textChannel.GetMessagesAsync(lastMessageId.Value, Direction.Before).FlattenAsync()
                    : await textChannel.GetMessagesAsync().FlattenAsync();

                var enumerable1 = batch.ToList();
                logger.LogInformation("/tldr fetched {BatchCount} messages", enumerable1.Count());

                var enumerable = batch as IMessage[] ?? enumerable1.ToArray();
                
                if (enumerable.Length == 0)
                    break;
                
                var foundUserMessage = false;
                foreach (var msg in enumerable)
                {
                    messagesChecked++;
                    
                    if (msg.Author.Id == command.User.Id && lastUserMessage == null)
                    {
                        lastUserMessage = msg;
                        foundUserMessage = true;
                        break;
                    }
                    
                    if (lastUserMessage == null && !msg.Author.IsBot && msg.Author.Id != command.User.Id && !string.IsNullOrWhiteSpace(msg.Content))
                    {
                        messages.Add(msg);
                    }
                }
                
                if (foundUserMessage)
                    break;
                
                lastMessageId = enumerable.Last().Id;
            }

            if (messages.Count == 0)
            {
                var noMsgText = lastUserMessage != null 
                    ? "No new messages to summarize since your last message."
                    : "No messages found to summarize in this channel (or this is your first message here).";
                await command.FollowupAsync(noMsgText);
                logger.LogInformation("/tldr found no messages to summarize");
                return;
            }

            messages.Reverse();
            
            var formattedMessages = messages.Select(FormatMessage).ToList();
            
            const int maxMessages = 50;
            if (formattedMessages.Count > maxMessages)
            {
                formattedMessages = formattedMessages.TakeLast(maxMessages).ToList();
            }

            logger.LogInformation("/tldr processing {MessageCount} messages", formattedMessages.Count);

            var summary = await openAi.SummarizeMessages(formattedMessages);

            if (string.IsNullOrWhiteSpace(summary))
            {
                await command.FollowupAsync("Unable to generate summary.");
                logger.LogInformation("/tldr failed to generate summary");
                return;
            }

            var timeInfo = lastUserMessage != null 
                ? $"since your last message ({FormatTimestamp(lastUserMessage.Timestamp)})"
                : "from recent conversation";
                
            var header = $"**📝 Summary {timeInfo}:**\n\n";
            var fullResponse = header + summary;

            foreach (var chunk in SplitMessage(fullResponse))
            {
                await command.FollowupAsync(chunk);
            }

            logger.LogInformation("/tldr completed successfully for {MessageCount} messages", messages.Count);
        }
        catch (Exception ex)
        {
            await command.FollowupAsync("Error generating summary.");
            logger.LogError(ex, "/tldr exception");
        }
    }

    private static string FormatMessage(IMessage message)
    {
        var timestamp = message.Timestamp.ToString("HH:mm");
        var author = message.Author.GlobalName ?? message.Author.Username;
        var content = message.Content;
        
        if (message.Reference?.MessageId.IsSpecified == true)
        {
            content = $"[Reply] {content}";
        }
        
        if (message.Attachments.Any())
        {
            var attachmentInfo = $" [Attachments: {string.Join(", ", message.Attachments.Select(a => a.Filename))}]";
            content += attachmentInfo;
        }
        
        return $"[{timestamp}] {author}: {content}";
    }

    private static string FormatTimestamp(DateTimeOffset timestamp)
    {
        var now = DateTimeOffset.UtcNow;
        var diff = now - timestamp;
        
        return diff.TotalDays >= 1 
            ? timestamp.ToString("MMM dd 'at' HH:mm") 
            : timestamp.ToString("'today at' HH:mm");
    }

    private static IEnumerable<string> SplitMessage(string text)
    {
        const int maxLength = 1900; 
        
        if (text.Length <= maxLength)
        {
            yield return text;
            yield break;
        }
        
        var lines = text.Split('\n');
        var sb = new StringBuilder();
        
        foreach (var line in lines)
        {
            if (sb.Length + line.Length + 1 > maxLength && sb.Length > 0)
            {
                yield return sb.ToString();
                sb.Clear();
            }
            
            if (line.Length > maxLength)
            {
                for (var i = 0; i < line.Length; i += maxLength)
                {
                    yield return line.Substring(i, Math.Min(maxLength, line.Length - i));
                }
            }
            else
            {
                if (sb.Length > 0)
                    sb.AppendLine();
                sb.Append(line);
            }
        }
        
        if (sb.Length > 0)
            yield return sb.ToString();
    }
}