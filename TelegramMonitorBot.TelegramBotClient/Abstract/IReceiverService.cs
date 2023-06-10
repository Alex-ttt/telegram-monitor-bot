namespace TelegramMonitorBot.TelegramBotClient.Abstract;

public interface IReceiverService
{
    Task ReceiveAsync(CancellationToken stoppingToken);
}