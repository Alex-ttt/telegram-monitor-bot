namespace TelegramMonitorBot.Storage;

internal static class DynamoDbConfig
{
    internal static class Users
    {
        internal const string TableName = "Users";
        internal const string PrimaryKeyName = "UserId";
    }
    
    internal static class Channels
    {
        internal const  string TableName = "Channels";
        internal const string PrimaryKeyName = "ChannelId";
    }


    internal static class UserChannels
    {
        internal  const string TableName = "UserChannels";
        internal const string HashKeyName = "UserId";
        internal const string RangeKeyName = "ChannelId";
    }
    
}
