namespace TelegramMonitorBot.Domain.Models;

public class UserChannel
{
    public UserChannel(long userId, long channelId, List<string>? phrases = null)
    {
        UserId = userId;
        ChannelId = channelId;
        Phrases = phrases;
    }

    public long UserId { get; }
    public long ChannelId { get; }
    public List<string>? Phrases { get; }
    
    public DateTimeOffset Created { get; init; } = DateTimeOffset.UtcNow;
}
