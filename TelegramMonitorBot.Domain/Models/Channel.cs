namespace TelegramMonitorBot.Domain.Models;

public class Channel
{
    public long ChannelId { get; }
    public string Name { get; }

    public Channel(long channelId, string name)
    {
        ChannelId = channelId;
        Name = name;
    }
}
