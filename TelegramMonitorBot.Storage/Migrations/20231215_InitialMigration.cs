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
        await CreateChannelsTable(amazonDynamoDbClient);
        await CreateUsersTable(amazonDynamoDbClient);
        await CreateUserChannelsTable(amazonDynamoDbClient);
        
        await Task.WhenAll(
            WaitForTableToBeActive(amazonDynamoDbClient, DynamoDbConfig.Users.TableName),
            WaitForTableToBeActive(amazonDynamoDbClient, DynamoDbConfig.Channels.TableName),
            WaitForTableToBeActive(amazonDynamoDbClient, DynamoDbConfig.UserChannels.TableName));
        
        await PutInitialData(amazonDynamoDbClient);
    }

    private async Task CreateUsersTable(IAmazonDynamoDB client)
    {
        var primaryKey = "UserId";
        var primaryKeyType = "N";

        var request = new CreateTableRequest
        {
            TableName = DynamoDbConfig.Users.TableName,
            AttributeDefinitions = { new AttributeDefinition {AttributeName = primaryKey, AttributeType = primaryKeyType}},
            KeySchema = { new KeySchemaElement {AttributeName = primaryKey, KeyType = "HASH"}},
            ProvisionedThroughput = new ProvisionedThroughput
            {
                ReadCapacityUnits = 5,
                WriteCapacityUnits = 3
            }
        };

        _ = await client.CreateTableAsync(request);
    }

    private async Task CreateChannelsTable(IAmazonDynamoDB client)
    {
        var primaryKey = "ChannelId";
        
        var request = new CreateTableRequest
        {
            TableName = DynamoDbConfig.Channels.TableName,
            AttributeDefinitions =
            {
                new AttributeDefinition {AttributeName = primaryKey, AttributeType = "N"}
            },
            KeySchema = {new KeySchemaElement {AttributeName = primaryKey, KeyType = "HASH"}},
            ProvisionedThroughput = new ProvisionedThroughput
            {
                ReadCapacityUnits = 5,
                WriteCapacityUnits = 3
            }
        };


        _ = await client.CreateTableAsync(request);
    }

    private async Task CreateUserChannelsTable(IAmazonDynamoDB client)
    {
        var primaryKey1 = "UserId";
        var primaryKey2 = "ChannelId";
        var request = new CreateTableRequest
        {
            TableName = DynamoDbConfig.UserChannels.TableName,

            AttributeDefinitions = 
            {
                new AttributeDefinition{ AttributeName = primaryKey1, AttributeType = "N" },
                new AttributeDefinition{ AttributeName = primaryKey2, AttributeType = "N" }
            },

            KeySchema = 
            {
                new KeySchemaElement{ AttributeName = primaryKey1, KeyType = "HASH" }, 
                new KeySchemaElement{ AttributeName = primaryKey2, KeyType = "RANGE" }
            },
            ProvisionedThroughput = new ProvisionedThroughput
            {
                ReadCapacityUnits = 5,
                WriteCapacityUnits = 3
            }
        };

        _ = await client.CreateTableAsync(request);
    }

    private async Task WaitForTableToBeActive(IAmazonDynamoDB client, string tableName)
    {
        var describeTableRequest = new DescribeTableRequest {TableName = tableName};

        while (true)
        {
            var describeTableResponse = await client.DescribeTableAsync(describeTableRequest);
            if (describeTableResponse.Table.TableStatus == "ACTIVE")
            {
                return;
            }

            Console.WriteLine(
                $"Waiting for {tableName} to be active. Current status: {describeTableResponse.Table.TableStatus}");
            await Task.Delay(1000);
        }
    }

    private async Task PutInitialData(IAmazonDynamoDB client)
    {
        var channels = new List<Channel>
        {
            new Channel(1, "Zero Channel"),
            new Channel(2, "Second Channel"),
        };
        
        var users = new List<User>()
        {
            new User(1, "Alex"),
            new User(2, "Petr"),
        };
        
        var userChannels = new List<UserChannel>()
        {
            new UserChannel(1, 2, new List<string>{"hello", "world"}) { Created = DateTimeOffset.Now },
            new UserChannel(2, 1, new List<string>(){"who", "am", "I"}) { Created = DateTimeOffset.Now },
        };
        
        var request = new BatchWriteItemRequest
        {
            RequestItems = new Dictionary<string, List<WriteRequest>>
            {
                [DynamoDbConfig.Users.TableName] = users.Select(GetUserWriteRequest).ToList(),
                [DynamoDbConfig.Channels.TableName] = channels.Select(GetChannelWriteRequest).ToList(),
                [DynamoDbConfig.UserChannels.TableName] = userChannels.Select(GetUserChannelsWriteRequest).ToList(),
            }
        };
        
        await client.BatchWriteItemAsync(request);
        
        WriteRequest GetUserWriteRequest(User user) => new(new PutRequest(user.ToDictionary()));
        WriteRequest GetChannelWriteRequest(Channel channel) => new(new PutRequest(channel.ToDictionary()));
        WriteRequest GetUserChannelsWriteRequest(UserChannel userChannels) => new(new PutRequest(userChannels.ToDictionary()));
    }
}
