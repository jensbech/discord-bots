using DiscordBots.BookStack;
using DiscordBots.OpenAI;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DiscordBots.BoredBot;

internal sealed class ServiceBotInitializer(
    IBookStackClient bookStackClient,
    IOpenAIClient openAIClient,
    ILogger<ServiceBotInitializer> logger
) : IHostedService
{
    private readonly IBookStackClient _bookStackClient = bookStackClient;
    private readonly IOpenAIClient _openAIClient = openAIClient;
    private readonly ILogger<ServiceBotInitializer> _logger = logger;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (BoredBot.Instance is null)
        {
            _logger.LogDebug("BoredBot instance not yet created when service initializer started");
        }
        else
        {
            BoredBot.Instance.SetBookStackClient(_bookStackClient);
            BoredBot.Instance.SetOpenAIClient(_openAIClient);
            _logger.LogInformation("BookStack and OpenAI clients injected into BoredBot");
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
                    BoredBot.Instance.SetBookStackClient(_bookStackClient);
                    BoredBot.Instance.SetOpenAIClient(_openAIClient);
                    _logger.LogInformation(
                        "BookStack and OpenAI clients injected into BoredBot after {Attempts} attempts",
                        attempts
                    );
                }
            },
            cancellationToken
        );

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
