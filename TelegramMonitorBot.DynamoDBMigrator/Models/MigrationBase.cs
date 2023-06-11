using Amazon.DynamoDBv2;

namespace TelegramMonitorBot.DynamoDBMigrator;

public abstract class MigrationBase
{
    private readonly List<AmazonDynamoDBRequest> _operations = new();

    internal IReadOnlyCollection<AmazonDynamoDBRequest> Operations => _operations.AsReadOnly();

    public abstract void Up();

    protected void AddRequest(AmazonDynamoDBRequest request)
    {
        _operations.Add(request);
    }
}