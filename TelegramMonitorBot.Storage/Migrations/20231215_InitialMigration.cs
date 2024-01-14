using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime.Documents;
using TelegramMonitorBot.Domain.Models;
using TelegramMonitorBot.DynamoDBMigrator;
using TelegramMonitorBot.DynamoDBMigrator.Models;
using TelegramMonitorBot.Storage.Mapping;

namespace TelegramMonitorBot.Storage.Migrations;

[Migration(20231215, "InitialMigration")]
public class InitialMigration : MigrationBase
{
    public override async Task Apply(IAmazonDynamoDB amazonDynamoDbClient)
    {
        await CreateTable(amazonDynamoDbClient);
    }

    private async Task CreateTable(IAmazonDynamoDB client)
    {
        var request = new CreateTableRequest
        {
            TableName = DynamoDbConfig.ChannelUsers.TableName,
            KeySchema = 
            {
                new KeySchemaElement{ AttributeName = DynamoDbConfig.ChannelUsers.PartitionKeyName, KeyType = "HASH" }, 
                new KeySchemaElement{ AttributeName = DynamoDbConfig.ChannelUsers.SortKeyName, KeyType = "RANGE" }
            },
            AttributeDefinitions = 
            {
                new AttributeDefinition{ AttributeName = DynamoDbConfig.ChannelUsers.PartitionKeyName, AttributeType = "S" },
                new AttributeDefinition{ AttributeName = DynamoDbConfig.ChannelUsers.SortKeyName, AttributeType = "S" }
            },
            GlobalSecondaryIndexes =
            {
                new GlobalSecondaryIndex()
                {
                    IndexName = DynamoDbConfig.ChannelUsers.GlobalSecondaryIndexName,
                    KeySchema =
                    {
                        new KeySchemaElement{ AttributeName = DynamoDbConfig.ChannelUsers.SortKeyName, KeyType = "HASH" },
                        new KeySchemaElement{ AttributeName = DynamoDbConfig.ChannelUsers.PartitionKeyName, KeyType = "RANGE" }, 
                    },
                    Projection = new Projection
                    {
                        ProjectionType = "INCLUDE",
                        NonKeyAttributes = { DynamoDbConfig.ChannelUsers.Attributes.ChannelUserCreated},
                    },
                    ProvisionedThroughput = new ProvisionedThroughput
                    {
                        ReadCapacityUnits = 5,
                        WriteCapacityUnits = 3
                    }
                }
            },
            ProvisionedThroughput = new ProvisionedThroughput
            {
                ReadCapacityUnits = 5,
                WriteCapacityUnits = 3
            },
        };
        
        _ = await client.CreateTableAsync(request);
    }
}
