using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TelegramMonitorBot.Storage.Repositories.Abstractions;
using TelegramMonitorBot.TelegramApiClient;

namespace TelegramMonitorBot.TelegramBotClient.Background;

public class UpdatePublisher : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ITelegramApiClient _telegramApiClient;

    public UpdatePublisher(
        IServiceScopeFactory serviceScopeFactory,
        ITelegramApiClient telegramApiClient)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _telegramApiClient = telegramApiClient;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (stoppingToken.IsCancellationRequested is false)
        {
            await HandleAsync(stoppingToken);
            await Task.Delay(1000, stoppingToken);
            // TODO Handle exception
        }
    }

    private async Task HandleAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var channelUserRepository = scope.ServiceProvider.GetRequiredService<IChannelUserRepository>();
        var channels = await channelUserRepository.GetAllChannelUsersRelations(stoppingToken);
        
        // Think about channel not found with TdLib API
    }
}