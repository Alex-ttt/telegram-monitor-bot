using Amazon.DynamoDBv2;

namespace TelegramMonitorBot.DynamoDBMigrator.Models;

public abstract class MigrationBase
{
    public abstract Task Apply(IAmazonDynamoDB amazonDynamoDbClient);
}
