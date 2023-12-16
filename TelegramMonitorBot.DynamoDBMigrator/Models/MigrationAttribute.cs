namespace TelegramMonitorBot.DynamoDBMigrator.Models;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class MigrationAttribute : Attribute
{
    public long MigrationId { get; }
    public string MigrationName { get; }

    public MigrationAttribute(long migrationId, string migrationName)
    {
        MigrationId = migrationId;
        MigrationName = migrationName;
    }
}
