namespace TelegramMonitorBot.DynamoDBMigrator;

public record MigrationInfo(MigrationBase Migration, MigrationAttribute Metadata);
