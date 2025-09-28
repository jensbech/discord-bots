using DiscordBots.BookStack;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DiscordBots.BoredBot;

public class BookStackBotInitializer(
    ILogger<BookStackBotInitializer> logger,
    IBookStackClient client
) : IHostedService
{
    private readonly ILogger<BookStackBotInitializer> _logger = logger;
    private readonly IBookStackClient _client = client;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (BoredBot.Instance is null)
        {
            _logger.LogDebug(
                "BoredBot instance not yet created when BookStack initializer started"
            );
        }
        else
        {
            BoredBot.Instance.SetBookStackClient(_client);
            _logger.LogInformation("BookStack client injected into BoredBot");
        }

        _ = Task.Run(
            async () =>
            {
                int attempts = 0;
                while (BoredBot.Instance is null && attempts < 10)
                {
                    await Task.Delay(500, cancellationToken);
                    attempts++;
                }
                if (BoredBot.Instance is not null)
                {
                    BoredBot.Instance.SetBookStackClient(_client);
                    _logger.LogInformation(
                        "BookStack client injected into BoredBot after {Attempts} attempts",
                        attempts
                    );
                }
            },
            cancellationToken
        );

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
