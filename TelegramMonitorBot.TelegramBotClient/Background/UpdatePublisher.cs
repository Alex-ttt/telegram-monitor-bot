using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TelegramMonitorBot.Storage.Repositories.Abstractions;
using TelegramMonitorBot.TelegramApiClient;

namespace TelegramMonitorBot.TelegramBotClient.Background;

public class UpdatePublisher : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ITelegramApiClient _telegramApiClient;
    private readonly ILogger<UpdatePublisher> _logger;

    public UpdatePublisher(
        IServiceScopeFactory serviceScopeFactory,
        ITelegramApiClient telegramApiClient,
        ILogger<UpdatePublisher> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _telegramApiClient = telegramApiClient;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (stoppingToken.IsCancellationRequested is false)
        {
            try
            {
                await HandleAsync(stoppingToken);
                await Task.Delay(1000, stoppingToken);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"An error occured during {nameof(UpdatePublisher)} work");
                await Task.Delay(2000, CancellationToken.None);
            }
        }
    }

    private async Task HandleAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var channelUserRepository = scope.ServiceProvider.GetRequiredService<IChannelUserRepository>();
        var channelsResponse = await channelUserRepository.GetAllChannelUsersRelations(true, stoppingToken);
        if(channelsResponse.Items.Count == 0)
        {
            return;
        }

        foreach (var channelUser in channelsResponse.Items.Where(t => t.Phrases?.Count > 0))
        {
            await channelUserRepository.CheckChannelWithUser(channelUser.Channel.ChannelId, channelUser.UserId, stoppingToken);
            var searchMessages = await _telegramApiClient.SearchMessages(channelUser.Channel.Name, channelUser.Phrases!);
            if (searchMessages is null)
            {
                
            }
        }
        
        // var searchMessages = await _telegramApiClient.SearchMessages(testChannel.Channel.Name, testChannel.Phrases!);

        // if (searchMessages is null)
        // {    
        //     return;
        // }
        
        

    }
}