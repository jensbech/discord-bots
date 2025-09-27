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
    private readonly ILogger<DiscordBotService<TBot>> _logger = logger;
    private readonly ILogger<TBot> _botLogger = botLogger;
    private readonly Func<
        BotEnvironmentVariables,
        SlashCommandBuilder[],
        ILogger<TBot>,
        Task<TBot>
    > _botFactory = botFactory;
    private readonly SlashCommandBuilder[] _commands = commands;
    private readonly string _botName = botName;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Starting {BotName} service...", _botName);

            var environmentVariables = DiscordBot.EnsureEnvironmentVariables();
            await _botFactory(environmentVariables, _commands, _botLogger);

            _logger.LogInformation("{BotName} service started successfully", _botName);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("{BotName} service is stopping...", _botName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {BotName} service", _botName);
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("{BotName} service stopped", _botName);
        await base.StopAsync(cancellationToken);
    }
}
