using TelegramMonitorBot.TelegramApiClient.Models;

namespace TelegramMonitorBot.TelegramApiClient;

public interface ITelegramApiClient
{
    Task DoStuff();
    Task<Channel?> FindChannelByName(string channel);
    Task<Channel?> GetChannel(long channelId);
}
