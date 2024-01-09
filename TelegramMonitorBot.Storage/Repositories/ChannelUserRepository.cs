using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using TelegramMonitorBot.Domain.Models;
using TelegramMonitorBot.Storage.Caching;
using TelegramMonitorBot.Storage.Mapping;
using TelegramMonitorBot.Storage.Repositories.Abstractions;
using TelegramMonitorBot.Storage.Repositories.Abstractions.Models;

namespace TelegramMonitorBot.Storage.Repositories;

internal class ChannelUserRepository : IChannelUserRepository
{
    private readonly AmazonDynamoDBClient _dynamoDbClient;
    private readonly StorageMemoryCache _memoryCache;

    public ChannelUserRepository(
        DynamoClientFactory clientFactory, 
        StorageMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
        _dynamoDbClient = clientFactory.GetClient();
    }

    // TODO consider using result
    public async Task<bool> CheckChannelWithUser(long channelId, long userId, CancellationToken cancellationToken = default)
    {
        var channelUser = await GetChannelUser(channelId, userId, cancellationToken);
        return channelUser is not null;
    }

    public async Task<Channel?> GetChannel(long channelId, CancellationToken cancellationToken = default)
    {
        if(_memoryCache.GetChannel(channelId) is { } channel)
        {
            return channel;
        }
        
        var getChannelRequest = new GetItemRequest
        {
            TableName = DynamoDbConfig.TableName,
            Key = Mapper.GetChannelKey(channelId)
        };
        
        var channelResult = await _dynamoDbClient.GetItemAsync(getChannelRequest, cancellationToken);
        
        var result =
            channelResult.Item?.Any() is true
                ? channelResult.Item.ToChannel()
                : null;
        
        if (result is not null)
        {
            _memoryCache.SetChannel(result);
        }
        
        return result;
    }
    

    public async Task<bool> PutUserChannel(User user, Channel channel, CancellationToken cancellationToken)
    {
        await Task.WhenAll(
            PutItemIfNotExists(channel.ToDictionary(), cancellationToken),
            PutItemIfNotExists(user.ToDictionary(), cancellationToken));
        
        var channelUser = new ChannelUser(channel, user);
        var channelUserAdded = await PutItemIfNotExists(channelUser.ToDictionary(), cancellationToken);

        if (channelUserAdded)
        {
            _memoryCache.ResetUserChannels(user.UserId);
            _memoryCache.ResetChannel(channel.ChannelId);
        }

        return channelUserAdded;
    }

    public async Task<PageResult<Channel>> GetChannels(long userId, Pager? pager, CancellationToken cancellationToken = default)
    {
        if(_memoryCache.GetUserChannels(userId) is { } cached)
        {
            return GetPageResult(cached, pager);
        }
        
        var userChannelsQueryRequest = new QueryRequest
        {
            TableName = DynamoDbConfig.TableName,
            IndexName = DynamoDbConfig.GlobalSecondaryIndexName,
            KeyConditionExpression = $"{DynamoDbConfig.SortKeyName} = :user_id_value and begins_with({DynamoDbConfig.PartitionKeyName}, :channel_prefix)",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":user_id_value", new AttributeValue { S = Mapper.UserIdToKeyValue(userId) } },
                { ":channel_prefix", new AttributeValue { S = Mapper.ChannelIdPrefix } }
            },
            ProjectionExpression = $"{DynamoDbConfig.PartitionKeyName}, {DynamoDbConfig.Attributes.ChannelUserCreated}"
        };
        
        var userChannelsResponse = await _dynamoDbClient.QueryAsync(userChannelsQueryRequest, cancellationToken);
        if (userChannelsResponse.Items.Any() is false)
        {
            _memoryCache.SetUserChannels(userId, Array.Empty<Channel>());
            return PageResult<Channel>.EmptyPageResult;
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

        var userSubscribedToChannelDate = 
            userChannelsResponse.Items
                .ToDictionary(
                    t => t[DynamoDbConfig.PartitionKeyName].S,
                    t => DateTimeOffset.Parse(t[DynamoDbConfig.Attributes.ChannelCreated].S));
        
        // TODO handle channelsResponse.UnprocessedKeys
        var channelsResponse = await _dynamoDbClient.BatchGetItemAsync(batchChannelsRequest, cancellationToken);
        var channels = channelsResponse.Responses[DynamoDbConfig.TableName]
            .OrderBy(OrderSelector)
            .Select(t => t.ToChannel())
            .ToList();
        
        _memoryCache.SetUserChannels(userId, channels);
        
        return GetPageResult(channels, pager);

        DateTimeOffset OrderSelector(Dictionary<string, AttributeValue> t) => userSubscribedToChannelDate[t[DynamoDbConfig.PartitionKeyName].S];
    }

    public async Task AddPhrases(ChannelUser channelUser, CancellationToken cancellationToken)
    {
        if (channelUser.Phrases is not { Count: > 0 } phrases)
        {
            _memoryCache.SetChannelUserPhrases(channelUser.ChannelId, channelUser.UserId, Array.Empty<string>());
            return;
        }
        
        var existedChannelUser = await GetChannelUser(channelUser.ChannelId, channelUser.UserId, cancellationToken);
        if (existedChannelUser is null)
        {
            return;
        }

        var newPhrases = existedChannelUser.Phrases is not { Count: > 0 }
            ? phrases.Distinct().ToList()
            : phrases.Union(existedChannelUser.Phrases).Distinct().ToList();

        await SetNewPhrases(channelUser, newPhrases, cancellationToken);
    }

    public async Task<ICollection<string>> GetChannelUserPhrases(long channelId, long userId, CancellationToken cancellationToken)
    {
        if (_memoryCache.GetChannelUserPhrases(channelId, userId) is { } cachedPhrases)
        {
            return cachedPhrases;
        }
        
        var channelUser = await GetChannelUser(channelId, userId, cancellationToken);
        ICollection<string> result = 
            channelUser?.Phrases?.Any() is true 
                ? channelUser.Phrases
                : Array.Empty<string>();
        
        _memoryCache.SetChannelUserPhrases(channelId, userId, result);
        
        return result;
    }

    public async Task RemovePhrase(long channelId, long userId, string phrase, CancellationToken cancellationToken)
    {
        var channelUser = await GetChannelUser(channelId, userId, cancellationToken);
        if (channelUser?.Phrases is null)
        {
            return;
        }
        
        channelUser.Phrases.Remove(phrase);
        await SetNewPhrases(channelUser, channelUser.Phrases, cancellationToken);
    }

    public async Task RemoveChannelUser(long channelId, long userId, CancellationToken cancellationToken)
    {
        var deleteItemRequest = new DeleteItemRequest
        {
            TableName = DynamoDbConfig.TableName,
            Key = Mapper.GetChannelUserKey(channelId, userId)
        };
        
        _memoryCache.ResetUserChannels(userId);
        _memoryCache.ResetChannelUserPhrases(channelId, userId);
        await _dynamoDbClient.DeleteItemAsync(deleteItemRequest, cancellationToken);
    }
    
    private async Task SetNewPhrases(ChannelUser channelUser, List<string> newPhrases, CancellationToken cancellationToken)
    {
        UpdateItemRequest updateItemRequest;
        if (newPhrases.Count == 0)
        {
            updateItemRequest = new UpdateItemRequest
            {
                TableName = DynamoDbConfig.TableName, 
                Key = Mapper.GetChannelUserKey(channelUser),
                ExpressionAttributeNames = new Dictionary<string, string> { { "#phrases", DynamoDbConfig.Attributes.ChannelUserPhrases } },
                UpdateExpression = "REMOVE #phrases",
            };
        }
        else
        {
            updateItemRequest = new UpdateItemRequest
            {
                TableName = DynamoDbConfig.TableName, 
                Key = Mapper.GetChannelUserKey(channelUser),
                ExpressionAttributeNames = new Dictionary<string, string> { { "#phrases", DynamoDbConfig.Attributes.ChannelUserPhrases } },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> { { ":newPhrases", new AttributeValue { SS = newPhrases } } },
                UpdateExpression = "SET #phrases = :newPhrases",
            };
        }
        
        await _dynamoDbClient.UpdateItemAsync(updateItemRequest, cancellationToken);
        _memoryCache.ResetChannelUserPhrases(channelUser.ChannelId, channelUser.UserId);
    }

    private async Task<ChannelUser?> GetChannelUser(long channelId, long userId, CancellationToken cancellationToken)
    {
        var getChannelUserRequest = new GetItemRequest
        {
            TableName = DynamoDbConfig.TableName,
            Key = Mapper.GetChannelUserKey(channelId, userId),
        };
        
        var channelUserResult = await _dynamoDbClient.GetItemAsync(getChannelUserRequest, cancellationToken);
        return 
            channelUserResult.Item?.Any() is true 
                ? channelUserResult.Item.ToChannelUser() 
                : null;
    }
    
    private async Task<bool> PutItemIfNotExists(Dictionary<string, AttributeValue> item, CancellationToken cancellationToken)
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
            return false;
        }

        return true;
    }
    
    private static PageResult<TItem> GetPageResult<TItem>(ICollection<TItem> items, Pager? pager)
    {
        return 
            pager is null 
                ? new PageResult<TItem>(items, new Pager()) 
                : new PageResult<TItem>(items, pager);
    }
}
