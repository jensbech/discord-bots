using Discord;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DiscordBots.Core;

public class BotService<TBot>(
    ILogger<BotService<TBot>> logger,
    ILogger<TBot> botLogger,
    IServiceProvider serviceProvider,
    SlashCommandBuilder[] commands
) : BackgroundService
    where TBot : BaseDiscordBot
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            logger.LogInformation("Starting bot service...");
            var bot = ActivatorUtilities.CreateInstance<TBot>(
                serviceProvider,
                commands,
                botLogger
            );
            await bot.InitializeAsync();
            logger.LogInformation("Bot service started successfully");
            _ = Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Bot service is stopping...");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in bot service");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Bot service stopped");
        await base.StopAsync(cancellationToken);
    }
}
