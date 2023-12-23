namespace TelegramMonitorBot.TelegramApiClient.Models;

public class Channel
{
    public long Id { get; set; }

    public string Title { get; set; }
    public long LastMessageId { get; set; }
}