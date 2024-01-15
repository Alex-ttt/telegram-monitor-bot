namespace TelegramMonitorBot.Domain.Models;

public class Channel
{
    public long ChannelId { get; }
    public string Name { get; }
    public DateTimeOffset Created { get; init; } = DateTimeOffset.UtcNow;

    public Channel(long channelId, string name)
    {
        ChannelId = channelId;
        Name = name;
    }

    public override int GetHashCode() => ChannelId.GetHashCode();
}
