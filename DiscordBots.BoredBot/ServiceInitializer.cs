using DiscordBots.BookStack;
using DiscordBots.OpenAI;
using Microsoft.Extensions.Hosting;

namespace DiscordBots.BoredBot;

internal sealed class ServiceInitializer(
    IBookStackClient bookStackClient,
    IOpenAiClient openAiClient
) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        BoredBot.Instance.SetBookStackClient(bookStackClient);
        BoredBot.Instance.SetOpenAiClient(openAiClient);
        
        _ = Task.Run(
            async () =>
            {
                var attempts = 0;
                while (BoredBot.Instance is null && attempts < 10)
                {
                    await Task.Delay(500, cancellationToken);
                    attempts++;
                }
                if (BoredBot.Instance is not null)
                {
                    BoredBot.Instance.SetBookStackClient(bookStackClient);
                    BoredBot.Instance.SetOpenAiClient(openAiClient);
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
