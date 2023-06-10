using Microsoft.Extensions.Logging;
using TelegramMonitorBot.TelegramBotClient.Abstract;

namespace TelegramMonitorBot.TelegramBotClient.Services;

// Compose Polling and ReceiverService implementations
public class PollingService : PollingServiceBase<ReceiverService>
{
    public PollingService(IServiceProvider serviceProvider, ILogger<PollingService> logger)
        : base(serviceProvider, logger)
    {
    }
}
