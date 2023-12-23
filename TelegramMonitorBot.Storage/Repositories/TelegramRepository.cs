using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using TelegramMonitorBot.Domain.Models;
using TelegramMonitorBot.Storage.Exceptions;
using TelegramMonitorBot.Storage.Mapping;
using TelegramMonitorBot.Storage.Repositories.Abstractions;

namespace TelegramMonitorBot.Storage.Repositories;

internal class TelegramRepository : ITelegramRepository
{
    private readonly AmazonDynamoDBClient _dynamoDbClient;

    public TelegramRepository(DynamoClientFactory clientFactory)
    {
        _dynamoDbClient = clientFactory.GetClient();
    }
    
    public async Task PutUserChannel(User user, Channel channel, CancellationToken cancellationToken)
    {
        var userId = user.UserId.ToString();
        var channelId = channel.ChannelId.ToString();
        var item = new Dictionary<string, AttributeValue>
        {
            {"UserId", new AttributeValue{N = userId}},
            {"ChannelId", new AttributeValue{N = channelId}}, 
            {"User", new AttributeValue 
                {
                    M = new Dictionary<string, AttributeValue> 
                    {
                        {"UserId", new AttributeValue {N = userId}},  
                        {"Name", new AttributeValue {S = user.Name}}
                    }
                }
            },
            {"Channel", new AttributeValue
                {
                    M = new Dictionary<string, AttributeValue>
                    {
                        {"ChannelId", new AttributeValue{N = channelId}},
                        {"Name", new AttributeValue{S = channel.Name}}  
                    }
                }
            }
        };

// Put item in table
        await _dynamoDbClient.PutItemAsync(DynamoDbConfig.UserChannels.TableName, item, cancellationToken);

        
        
        
        var transactionRequest = new TransactWriteItemsRequest
        {
            TransactItems =
            {
                new TransactWriteItem
                {
                    Put = new Put
                    {
                        TableName = DynamoDbConfig.Users.TableName,
                        Item = user.ToDictionary()
                    }
                },
                new TransactWriteItem
                {
                    Put = new Put
                    {
                        TableName = DynamoDbConfig.Channels.TableName,
                        Item = channel.ToDictionary(),
                    }
                },
                new TransactWriteItem
                {
                    Put = new Put
                    {
                        TableName = DynamoDbConfig.UserChannels.TableName,
                        ConditionExpression = "attribute_not_exists(UserId) AND attribute_not_exists(ChannelId)",
                        Item = new UserChannel(user.UserId, channel.ChannelId).ToDictionary(),
                    }
                }
            }
        };

        try
        {
            await _dynamoDbClient.TransactWriteItemsAsync(transactionRequest, cancellationToken);
        }
        catch (TransactionCanceledException ex) 
            when (ex.CancellationReasons.Any(t => t.Code == "ConditionalCheckFailed"))
        {
            throw new UserChannelAlreadyExistsException(user.UserId, channel.ChannelId); 
        }
    }

    public async Task<ICollection<Channel>> GetChannels(long userId, CancellationToken cancellationToken = default)
    {
        var userChannelsQueryRequest = new QueryRequest
        {
            TableName = DynamoDbConfig.UserChannels.TableName,
            KeyConditionExpression = "#UserId = :value",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                { "#UserId", "UserId" }
            },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":value", new AttributeValue { N = userId.ToString() } }
            },
        };
        
        
        var userChannelsResponse = await _dynamoDbClient.QueryAsync(userChannelsQueryRequest, cancellationToken);
        if (userChannelsResponse.Items.Any() is false)
        {
            return Array.Empty<Channel>();
        }

        var batchChannelsRequest = new BatchGetItemRequest
        {
            RequestItems = new Dictionary<string, KeysAndAttributes>
            {
                [DynamoDbConfig.Channels.TableName] = new()
                {
                    Keys = userChannelsResponse.Items
                        .Select(t => new Dictionary<string, AttributeValue>
                        {
                            [DynamoDbConfig.Channels.PrimaryKeyName] = new () { N = t[DynamoDbConfig.Channels.PrimaryKeyName].N}
                        })
                        .ToList(),
                }
            }
        };

        var channelsResponse = await _dynamoDbClient.BatchGetItemAsync(batchChannelsRequest, cancellationToken);
        var channels = channelsResponse.Responses[DynamoDbConfig.Channels.TableName]
            .Select(t => t.ToChannel())
            .ToList();
        
        return channels;
    }

    // private async Task Text()
    // {
    //     // Create a list of primary key objects 
    //     List<Dictionary<string, AttributeValue>> keys = new List<Dictionary<string, AttributeValue>>();
    //
    //     // Add each item to retrieve
    //     keys.Add(new Dictionary<string, AttributeValue>
    //     {
    //        {"ChannelId", new AttributeValue{N = "1"}} 
    //     });
    //
    //     keys.Add(new Dictionary<string, AttributeValue>
    //     {
    //        {"ChannelId", new AttributeValue{N = "2"}}
    //     });
    //
    //     // Create the BatchGetItem request
    //     BatchGetItemRequest request = new BatchGetItemRequest
    //     {
    //        RequestItems = 
    //        {
    //           { 
    //              "Channels", // Table name
    //              keys 
    //           }
    //        }
    //     };
    //
    //     // Call DynamoDB client
    //     var response = await dynamoDBClient.BatchGetItemAsync(request);
    //
    //     // Deserialize the results
    //     ponse.Responses["Channels"];
    //
    // }
}
