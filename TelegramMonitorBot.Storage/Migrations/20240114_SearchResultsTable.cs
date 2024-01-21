using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using TelegramMonitorBot.Domain.Models;
using TelegramMonitorBot.DynamoDBMigrator.Models;
using TelegramMonitorBot.Storage.Mapping;

namespace TelegramMonitorBot.Storage.Migrations;

[Migration(20240114, "SearchResultsTable")]
public class SearchResultsTable : MigrationBase
{
    public override async Task Apply(IAmazonDynamoDB amazonDynamoDbClient)
    {
        var createTableRequest = new CreateTableRequest
        {
            TableName = DynamoDbConfig.SearchResults.TableName,
            KeySchema = 
            {
                new KeySchemaElement{ AttributeName = DynamoDbConfig.SearchResults.PartitionKeyName, KeyType = "HASH" }, 
                new KeySchemaElement{ AttributeName = DynamoDbConfig.SearchResults.SortKeyName, KeyType = "RANGE" }
            },
            AttributeDefinitions = 
            {
                new AttributeDefinition{ AttributeName = DynamoDbConfig.SearchResults.PartitionKeyName, AttributeType = "S" },
                new AttributeDefinition{ AttributeName = DynamoDbConfig.SearchResults.SortKeyName, AttributeType = "S" },
            },
            BillingMode = BillingMode.PAY_PER_REQUEST,
        };
        
        await amazonDynamoDbClient.CreateTableAsync(createTableRequest);
        
        var ttlRequest = new UpdateTimeToLiveRequest
        {
            TableName = DynamoDbConfig.SearchResults.TableName,
            TimeToLiveSpecification = new TimeToLiveSpecification()
            {
                AttributeName = DynamoDbConfig.SearchResults.Attributes.ExpiredAt,
                Enabled = true
            }
        };
        
        await amazonDynamoDbClient.UpdateTimeToLiveAsync(ttlRequest);
    }
}
