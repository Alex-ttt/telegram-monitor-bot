namespace TelegramMonitorBot.Storage;

internal static class DynamoDbConfig
{
    internal const string TableName = "ChannelUsers";
    internal const string GlobalSecondaryIndexName = "ChannelUsers";
    
    internal const string PartitionKeyName = "PartitionKey";
    internal const string SortKeyName = "SortKey";

    internal static class Attributes
    {
        internal static string ChannelName => "Name";
        internal static string ChannelCreated => "Created";
        internal static string UserName => "Name";
        internal static string UserCreated => "Created";
        
        internal static string ChannelUserCreated => "Created";
        internal static string ChannelUserPhrases => "Phrases";

    }
}
