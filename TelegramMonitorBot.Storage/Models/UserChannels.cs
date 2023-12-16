namespace TelegramMonitorBot.Storage.Models;

public class UserChannels
{
    public UserChannels()
    {
    }

    public UserChannels(long userId, long channelId, List<string> phrases)
    {
        UserId = userId;
        ChannelId = channelId;
        Phrases = phrases;
    }

    public long UserId { get; set; }
    public long ChannelId { get; set; }
    public List<string> Phrases { get; set; }
}
