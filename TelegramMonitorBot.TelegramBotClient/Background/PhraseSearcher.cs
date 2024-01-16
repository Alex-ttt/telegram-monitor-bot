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

    public PhraseSearcher(
        IMediator mediator, 
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
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"An error occured during {nameof(PhraseSearcher)} work");
                await Task.Delay(2500, CancellationToken.None);
            }
        }
    }
}
