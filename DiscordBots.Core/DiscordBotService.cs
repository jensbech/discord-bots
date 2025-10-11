using Discord;
using Microsoft.Extensions.DependencyInjection; 
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DiscordBots.Core;

public class DiscordBotService<TBot>(
    ILogger<DiscordBotService<TBot>> logger,
    ILogger<TBot> botLogger,
    IServiceProvider serviceProvider,
    SlashCommandBuilder[] commands,
    string botName
) : BackgroundService where TBot : BaseDiscordBot
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            logger.LogInformation("Starting {BotName} service...", botName);
            var environmentVariables = BaseDiscordBot.EnsureEnvironmentVariables();
            var bot = ActivatorUtilities.CreateInstance<TBot>(
                serviceProvider,
                environmentVariables.DiscordBotToken,
                commands,
                botLogger
            );
            await bot.InitializeAsync(botName);
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
