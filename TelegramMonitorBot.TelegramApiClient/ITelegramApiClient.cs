using TelegramMonitorBot.TelegramApiClient.Models;

namespace TelegramMonitorBot.TelegramApiClient;

public interface ITelegramApiClient
{
    Task<Channel?> FindChannelByName(string channel);
    Task<ChannelSearchMessages?> SearchMessages(string channelName, ICollection<string> phrases);
}
