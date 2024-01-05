namespace TelegramMonitorBot.Domain.Models;

public class ChannelUser
{
    public ChannelUser(long channelId, long userId, List<string>? phrases = null)
    {
        UserId = userId;
        ChannelId = channelId;
        Phrases = phrases;
    }
    
    public ChannelUser(Channel channel, User user, List<string>? phrases = null) 
        : this(channel.ChannelId, user.UserId, phrases)
    {
    }

    public long UserId { get; }
    public long ChannelId { get; }
    public List<string>? Phrases { get; private set; }
    public DateTimeOffset Created { get; init; } = DateTimeOffset.UtcNow;

    public void AddPhrases(IEnumerable<string> phrases)
    {
        if (Phrases is null)
        {
            Phrases = phrases.Distinct().ToList();
        }
        else
        {
            Phrases.AddRange(phrases.Where(t => Phrases.Contains(t) is false));
        }
    }
    
    public void RemovePhrases(IEnumerable<string> phrases)
    {
        Phrases?.RemoveAll(phrases.Contains);
    }
    
    public void RemovePhrase(string phrase)
    {
        Phrases?.Remove(phrase);
    }
}
