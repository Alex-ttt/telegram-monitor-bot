namespace TelegramMonitorBot.Domain.Models;

public class SearchResults
{
    public SearchResults(long channelId, long userId, IDictionary<string, IList<Message>> results)
    {
        ChannelId = channelId;
        UserId = userId;
        Results = results;
    }
    
    public long ChannelId { get; }
    public long UserId { get; }
 
    public IDictionary<string, IList<Message>> Results { get; }
}
