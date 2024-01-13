namespace TelegramMonitorBot.Domain.Models;

public class ChannelUser
{
    public ChannelUser(long channelId, long userId, List<string>? phrases = null, long? lastMessage = null)
    {
        UserId = userId;
        ChannelId = channelId;
        Phrases = phrases;
        LastMessage = lastMessage;
    }

    public long UserId { get; }
    public long ChannelId { get; }
    public List<string>? Phrases { get; private set; }
    public DateTimeOffset Created { get; init; } = DateTimeOffset.UtcNow;
    public long? LastMessage { get; }
}
