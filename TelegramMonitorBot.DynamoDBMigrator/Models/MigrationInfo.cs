namespace TelegramMonitorBot.DynamoDBMigrator.Models;

public record MigrationInfo(MigrationBase Migration, MigrationAttribute Metadata);
