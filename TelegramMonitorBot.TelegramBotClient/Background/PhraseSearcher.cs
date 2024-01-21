using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TelegramMonitorBot.TelegramBotClient.Application.PhrasesSearch.Commands.SearchPhrases;

namespace TelegramMonitorBot.TelegramBotClient.Background;

public class PhraseSearcher : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<PhraseSearcher> _logger;
    
    private readonly TimeSpan _delayTime = TimeSpan.FromMinutes(1);

    public PhraseSearcher(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<PhraseSearcher> logger)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (stoppingToken.IsCancellationRequested is false)
        {
            try
            {
                var mediator = _serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Send(new SearchPhrasesRequest(), stoppingToken);
                await Task.Delay(_delayTime, stoppingToken);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "An error occured during {ServiceName} work", nameof(PhraseSearcher));
                await Task.Delay(2500, CancellationToken.None);
            }
        }
    }
}
