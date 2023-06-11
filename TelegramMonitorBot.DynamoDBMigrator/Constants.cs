namespace TelegramMonitorBot.DynamoDBMigrator;

internal static class Constants
{
    internal static class MigrationHistory
    {
        internal const string TableName = "__MigrationHistory";
        internal const string IdColumnName = "id";
        internal const string CreatedColumnName = "created";
        internal const string NameColumnName = "name";
        internal const string SourceColumnName = "source";
    }
}