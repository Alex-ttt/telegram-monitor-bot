namespace TelegramMonitorBot.Domain.Models;

public class SearchResults
{
    public SearchResults(long channelId, long userId, ICollection<SearchResult> results)
    {
        ChannelId = channelId;
        UserId = userId;
        Results = results;
    }
    
    public long ChannelId { get; }
    public long UserId { get; }
    public ICollection<SearchResult> Results { get; }

}