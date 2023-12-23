using System.Reflection;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using TelegramMonitorBot.DynamoDBMigrator.Models;

namespace TelegramMonitorBot.DynamoDBMigrator;

public class StorageMigrator
{
    private readonly AmazonDynamoDBClient _client;

    public StorageMigrator(AmazonDynamoDBClient client)
    {
        _client = client;
    }
    
    public async Task MigrateStorage(CancellationToken cancellationToken = default)
    {
        await CreateMigrationTable(cancellationToken);

        foreach (var migrationInfo in GetMigrationsInfo())
        {
            if (await CheckMigrationApplied(migrationInfo, cancellationToken))
            {
                continue;
            }
            
            await ApplyMigration(migrationInfo);
            await MarkMigrationApplied(migrationInfo);
        }
    }

    private async Task MarkMigrationApplied(MigrationInfo migrationInfo)
    {
        var tableName = Constants.MigrationHistory.TableName;
        var request = new PutItemRequest
        {
            TableName = tableName,
            Item = new Dictionary<string, AttributeValue>
            {
                [Constants.MigrationHistory.IdColumnName] = new() { N = migrationInfo.Metadata.MigrationId.ToString() },
                [Constants.MigrationHistory.NameColumnName] = new() { S = migrationInfo.Metadata.MigrationName },
                [Constants.MigrationHistory.SourceColumnName] = new() { S = migrationInfo.Migration.GetType().FullName },
                [Constants.MigrationHistory.CreatedColumnName] = new() { S = DateTimeOffset.UtcNow.ToString() },
            }
        };

        await _client.PutItemAsync(request);
    }


    private async Task CreateMigrationTable(CancellationToken cancellationToken)
    {
        var listTablesRequest = new ListTablesRequest(); 
        var listTablesResponse = await _client.ListTablesAsync(listTablesRequest, cancellationToken);
        if (listTablesResponse.TableNames.Contains(Constants.MigrationHistory.TableName))
        {
            return;
        }

        var attributeDefinitions = new List<AttributeDefinition>
        {
            new(Constants.MigrationHistory.IdColumnName, ScalarAttributeType.N),
            new(Constants.MigrationHistory.NameColumnName, ScalarAttributeType.S),
        };

        var keySchema = new List<KeySchemaElement>
        {
            new(Constants.MigrationHistory.IdColumnName, KeyType.HASH),
            new(Constants.MigrationHistory.NameColumnName, KeyType.RANGE),
        };

        var createTableRequest = new CreateTableRequest
        {
            TableName = Constants.MigrationHistory.TableName,
            AttributeDefinitions = attributeDefinitions,
            KeySchema = keySchema,
            ProvisionedThroughput = new ProvisionedThroughput
            {
                ReadCapacityUnits = 2,
                WriteCapacityUnits = 2
            }
        };

        await _client.CreateTableAsync(createTableRequest, cancellationToken);
    }

    private static IEnumerable<MigrationInfo> GetMigrationsInfo()
    {
        var migrationTypes = AppDomain
            .CurrentDomain
            .GetAssemblies()
            .SelectMany(t => t.GetTypes())
            .Where(t =>
                t.IsAbstract is false
                && t.IsAssignableTo(typeof(MigrationBase)))
            .Select(t => new {Type = t, Metadata = t.GetCustomAttribute<MigrationAttribute>()})
            .Where(t => t.Metadata is not null)
            .ToArray();

        var nonUniqueMigrations =
            migrationTypes
                .GroupBy(t => t.Metadata!.MigrationId)
                .Where(t => t.Count() > 1)
                .Select(t => 
                    $"Migration Id: {t.Key}. Migrations' names: {string.Join(", ", t.Select(t => t.Metadata.MigrationName))}")
                .ToArray();

        if (nonUniqueMigrations.Length > 0)
        {
            throw new InvalidOperationException($"None unique migrations ids found:\n\t {string.Join("\n\t", nonUniqueMigrations)}");
        }

        foreach (var typeInfo in migrationTypes.OrderBy(t => t.Metadata.MigrationId))
        {
            yield return new MigrationInfo((MigrationBase) Activator.CreateInstance(typeInfo.Type)!, typeInfo.Metadata!);
        }
    }

    private async Task ApplyMigration(MigrationInfo migrationInfo)
    {
        await migrationInfo.Migration.Apply(_client);
    }

    private async Task<bool> CheckMigrationApplied(MigrationInfo migrationInfo, CancellationToken cancellationToken)
    {
        const string keyConditionExpression = $"{Constants.MigrationHistory.IdColumnName} = :id";
        var expressionAttributeValues = new Dictionary<string, AttributeValue>
        { 
            { ":id", new AttributeValue { N = migrationInfo.Metadata.MigrationId.ToString() } }
        };
        
        var request = new QueryRequest
        {
            TableName = Constants.MigrationHistory.TableName,
            KeyConditionExpression = keyConditionExpression,
            ExpressionAttributeValues = expressionAttributeValues,
            Limit = 1
        };

        var response = await _client.QueryAsync(request, cancellationToken);

        return response.Items.Count > 0;
    }
}
