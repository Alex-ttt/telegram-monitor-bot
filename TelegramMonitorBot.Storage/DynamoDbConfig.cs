namespace TelegramMonitorBot.Storage;

internal static class DynamoDbConfig
{
    internal static class ChannelUsers
    {
        internal const string TableName = "ChannelUsers";
        internal const string GlobalSecondaryIndexName = "ChannelUsers";
    
        internal const string PartitionKeyName = "PartitionKey";
        internal const string SortKeyName = "SortKey";

        internal static class Attributes
        {
            internal const string ChannelName = "Name";
            internal const string ChannelCreated = "Created";
            internal const string UserName = "Name";
            internal const string UserCreated = "Created";
            internal const string ChannelUserCreated = "Created";
            internal const string ChannelUserPhrases = "Phrases";
            internal const string ChannelUserLastMessage = "LastMessage";
        }
    }
    
    internal static class SearchResults
    {
        internal const string TableName = "SearchResults";
        
        internal const string PartitionKeyName = "PartitionKey";
        internal const string SortKeyName = "SortKey";

        internal static class Attributes
        {
            internal const string SearchResults = "SearchResults";
            
            internal const string SearchResultsPhrase = "Phrase";
            internal const string SearchResultsMessages = "Messages";
            internal const string SearchResultsMessageId = "Id";
            internal const string SearchResultsMessageLink = "Link";
            internal const string SearchResultsMessageDate = "Date";
            
            internal const string Ttl = "TTL";
        }
    }
}
//
// public class QSearchResult
// {
//     public required string Phrase { get; init; }
//     public required List<Message> Messages { get; init; }
// }
//
// public record Message(long Id, string Link, DateTimeOffset Date);
