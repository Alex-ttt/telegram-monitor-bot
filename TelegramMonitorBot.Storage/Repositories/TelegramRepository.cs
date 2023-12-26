using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using TelegramMonitorBot.Domain.Models;
using TelegramMonitorBot.Storage.Caching;
using TelegramMonitorBot.Storage.Mapping;
using TelegramMonitorBot.Storage.Repositories.Abstractions;

namespace TelegramMonitorBot.Storage.Repositories;

internal class TelegramRepository : ITelegramRepository
{
    private readonly AmazonDynamoDBClient _dynamoDbClient;
    private readonly StorageMemoryCache _memoryCache;

    public TelegramRepository(
        DynamoClientFactory clientFactory, 
        StorageMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
        _dynamoDbClient = clientFactory.GetClient();
    }

    // TODO consider using result
    public async Task<bool> PutUserChannel(User user, Channel channel, CancellationToken cancellationToken)
    {
        await Task.WhenAll(
            PutIfNotExists(channel.ToDictionary(), cancellationToken),
            PutIfNotExists(user.ToDictionary(), cancellationToken));
        
        var channelUser = new ChannelUser( channel, user);
        var channelUserAdded = await PutIfNotExists(channelUser.ToDictionary(), cancellationToken);

        if (channelUserAdded)
        {
            _memoryCache.ResetUserChannels(user.UserId);
        }

        return channelUserAdded;
    }

    public async Task<ICollection<Channel>> GetChannels(long userId, CancellationToken cancellationToken = default)
    {
        if(_memoryCache.GetUserChannels(userId) is {} cached)
        {
            return cached;
        }
        
        var userChannelsQueryRequest = new QueryRequest
        {
            TableName = DynamoDbConfig.TableName,
            IndexName = DynamoDbConfig.GlobalSecondaryIndexName,
            KeyConditionExpression = $"{DynamoDbConfig.SortKeyName} = :user_id_value and begins_with({DynamoDbConfig.PartitionKeyName}, :channel_prefix)",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":user_id_value", new AttributeValue { S = ModelsMapper.UserIdToKeyValue(userId) } },
                { ":channel_prefix", new AttributeValue { S = ModelsMapper.ChannelIdPrefix } }
            },
            ProjectionExpression = $"{DynamoDbConfig.PartitionKeyName}"
        };
        
        var userChannelsResponse = await _dynamoDbClient.QueryAsync(userChannelsQueryRequest, cancellationToken);
        if (userChannelsResponse.Items.Any() is false)
        {
            _memoryCache.SetUserChannels(userId, Array.Empty<Channel>());
            return Array.Empty<Channel>();
        }
        
        var batchChannelsRequest = new BatchGetItemRequest
        {
            RequestItems = new Dictionary<string, KeysAndAttributes>
            {
                [DynamoDbConfig.TableName] = new()
                {
                    Keys = userChannelsResponse.Items
                        .Select(t => new Dictionary<string, AttributeValue>
                        {
                            [DynamoDbConfig.PartitionKeyName] = t[DynamoDbConfig.PartitionKeyName],
                            [DynamoDbConfig.SortKeyName] = t[DynamoDbConfig.PartitionKeyName],
                        })
                        .ToList(),
                }
            }
        };

        // TODO handle channelsResponse.UnprocessedKeys
        var channelsResponse = await _dynamoDbClient.BatchGetItemAsync(batchChannelsRequest, cancellationToken);
        var channels = channelsResponse.Responses[DynamoDbConfig.TableName]
            .Select(t => t.ToChannel())
            .ToList();
        
        _memoryCache.SetUserChannels(userId, channels);
        
        return channels;
    }
    
    private async Task<bool> PutIfNotExists(Dictionary<string, AttributeValue> item, CancellationToken cancellationToken)
    {
        var channelRequest = new PutItemRequest
        {
            TableName = DynamoDbConfig.TableName,
            Item = item,
            ConditionExpression = $"attribute_not_exists({DynamoDbConfig.PartitionKeyName}) AND attribute_not_exists({DynamoDbConfig.SortKeyName})",
        };

        try
        {
            await _dynamoDbClient.PutItemAsync(channelRequest, cancellationToken);
        }
        catch (ConditionalCheckFailedException ex) when(ex.ErrorCode is "ConditionalCheckFailedException")
        {
            Console.WriteLine(ex.Message);
            return false;
        }

        return true;
    }
}
