namespace TelegramMonitorBot.Domain.Models;

public class SearchResults
{
    private static IDictionary<string, IList<Message>>? _emptyResults;
    
    public long ChannelId { get; }
    public long UserId { get; }
 
    public IDictionary<string, IList<Message>> Results { get; }

    public int Version { get; }
    
    public SearchResults(long channelId, long userId, IDictionary<string, IList<Message>> results, int version = 1)
    {
        ChannelId = channelId;
        UserId = userId;
        Results = results;
        Version = version;
    }
    
    public static SearchResults GetEmpty(long channelId, long userId)
    {
        _emptyResults ??= new Dictionary<string, IList<Message>>();
        return new SearchResults(channelId, userId, _emptyResults);
    }
}
