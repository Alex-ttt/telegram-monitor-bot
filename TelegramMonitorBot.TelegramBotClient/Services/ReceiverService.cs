using Microsoft.Extensions.Logging;
using Telegram.Bot;
using TelegramMonitorBot.TelegramBotClient.Abstract;

namespace TelegramMonitorBot.TelegramBotClient.Services;

// Compose Receiver and UpdateHandler implementation
public class ReceiverService : ReceiverServiceBase<UpdateHandler>
{
    public ReceiverService(
        ITelegramBotClient botClient,
        UpdateHandler updateHandler,
        ILogger<ReceiverService> logger)
        : base(botClient, updateHandler, logger)
    {
    }
}
