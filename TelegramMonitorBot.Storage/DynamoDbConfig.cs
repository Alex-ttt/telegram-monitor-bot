namespace TelegramMonitorBot.Storage;

internal static class DynamoDbConfig
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
