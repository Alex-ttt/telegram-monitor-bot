using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime.Documents;
using TelegramMonitorBot.DynamoDBMigrator;
using TelegramMonitorBot.DynamoDBMigrator.Models;
using TelegramMonitorBot.Storage.Models;

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
            WaitForTableToBeActive(amazonDynamoDbClient, "Users"),
            WaitForTableToBeActive(amazonDynamoDbClient, "Channels"),
            WaitForTableToBeActive(amazonDynamoDbClient, "UserChannels"));

        var region = amazonDynamoDbClient.Config.AuthenticationRegion;
        
        await PutInitialData(amazonDynamoDbClient);
    }

    private async Task CreateUsersTable(IAmazonDynamoDB client)
    {
        var tableName = "Users";
        var primaryKey = "UserId";
        var primaryKeyType = "N";

        var request = new CreateTableRequest
        {
            TableName = tableName,
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
        var tableName = "Channels";
        var primaryKey = "ChannelId";
        
        var request = new CreateTableRequest
        {
            TableName = tableName,
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
        var tableName = "UserChannels";
        var primaryKey1 = "UserId";
        var primaryKey2 = "ChannelId";
        var request = new CreateTableRequest
        {
            TableName = tableName,

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
            new Channel() {ChannelId = 1, Name = "Zero Channel", },
            new Channel() {ChannelId = 2, Name = "Second Channel",},
        };
        
        var users = new List<User>()
        {
            new User(){ UserId = 1, Name = "Alex" },
            new User(){ UserId = 2, Name = "Noname"},
        };
        
        var userChannels = new List<UserChannels>()
        {
            new UserChannels(1, 2, new List<string>{"hello", "world"}),
            new UserChannels(2, 1, new List<string>(){"who", "am", "I"}),
        };
        
        var request = new BatchWriteItemRequest
        {
            RequestItems = new Dictionary<string, List<WriteRequest>>
            {
                ["Users"] = users.Select(GetUserPutRequest).ToList(),
                ["Channels"] = channels.Select(GetChannelPutRequest).ToList(),
                ["UserChannels"] = userChannels.Select(GetUserChannelsPutRequest).ToList(),
            }
        };
        
        await client.BatchWriteItemAsync(request);
        
        // TODO Move to special place
        WriteRequest GetUserPutRequest(User user)
        {
            return new WriteRequest(
                new PutRequest(new() 
                {
                    [nameof(User.UserId)] = new AttributeValue { N = user.UserId.ToString()},
                    [nameof(User.Name)] = new AttributeValue(user.Name),
                }));
        }
        
        WriteRequest GetChannelPutRequest(Channel channel)
        {
            return new WriteRequest(
                new PutRequest(new() 
                {
                    [nameof(Channel.ChannelId)] = new () { N = channel.ChannelId.ToString()},
                    [nameof(Channel.Name)] = new() { S = channel.Name},
                }));
        }
        
        WriteRequest GetUserChannelsPutRequest(UserChannels userChannels)
        {
            return new WriteRequest(
                new PutRequest(new() 
                {
                    [nameof(UserChannels.ChannelId)] = new AttributeValue { N = userChannels.ChannelId.ToString()},
                    [nameof(UserChannels.UserId)] = new AttributeValue { N = userChannels.UserId.ToString()},
                    [nameof(UserChannels.Phrases)] = new AttributeValue { SS = userChannels.Phrases},
                }));
        }
    }
}
