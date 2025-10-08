using Discord;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DiscordBots.Core;

public class DiscordBotService<TBot>(
    ILogger<DiscordBotService<TBot>> logger,
    ILogger<TBot> botLogger,
    Func<BotEnvironmentVariables, SlashCommandBuilder[], ILogger<TBot>, Task<TBot>> botFactory,
    SlashCommandBuilder[] commands,
    string botName
) : BackgroundService
    where TBot : DiscordBot
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            logger.LogInformation("Starting {BotName} service...", botName);
            var environmentVariables = DiscordBot.EnsureEnvironmentVariables();
            await botFactory(environmentVariables, commands, botLogger);
            logger.LogInformation("{BotName} service started successfully", botName);
            _ = Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("{BotName} service is stopping...", botName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in {BotName} service", botName);
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("{BotName} service stopped", botName);
        await base.StopAsync(cancellationToken);
    }
}
