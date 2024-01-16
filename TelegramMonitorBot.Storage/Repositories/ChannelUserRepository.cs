using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using TelegramMonitorBot.Domain.Models;
using TelegramMonitorBot.Storage.Caching;
using TelegramMonitorBot.Storage.Mapping;
using TelegramMonitorBot.Storage.Repositories.Abstractions;
using TelegramMonitorBot.Storage.Repositories.Abstractions.Models;
using TelegramMonitorBot.Storage.Repositories.Models;

using ChannelUsersConfig = TelegramMonitorBot.Storage.DynamoDbConfig.ChannelUsers;
using Mapper = TelegramMonitorBot.Storage.Mapping.ChannelUsersMapper;

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
        // TODO https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/ql-functions.exists.html
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
            TableName = ChannelUsersConfig.TableName,
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
        
        var channelUser = new ChannelUser(channel.ChannelId, user.UserId);
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
            TableName = ChannelUsersConfig.TableName,
            IndexName = ChannelUsersConfig.GlobalSecondaryIndexName,
            KeyConditionExpression = $"{ChannelUsersConfig.SortKeyName} = :user_id_value and begins_with({ChannelUsersConfig.PartitionKeyName}, :channel_prefix)",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":user_id_value", new AttributeValue { S = Mapper.UserIdToKeyValue(userId) } },
                { ":channel_prefix", new AttributeValue { S = Mapper.ChannelIdPrefix } }
            },
            ProjectionExpression = $"{ChannelUsersConfig.PartitionKeyName}, {ChannelUsersConfig.Attributes.ChannelUserCreated}"
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
                [ChannelUsersConfig.TableName] = new()
                {
                    Keys = userChannelsResponse.Items
                        .Select(t => new Dictionary<string, AttributeValue>
                        {
                            [ChannelUsersConfig.PartitionKeyName] = t[ChannelUsersConfig.PartitionKeyName],
                            [ChannelUsersConfig.SortKeyName] = t[ChannelUsersConfig.PartitionKeyName],
                        })
                        .ToList(),
                }
            }
        };

        var userSubscribedToChannelDate = 
            userChannelsResponse.Items
                .ToDictionary(
                    t => t[ChannelUsersConfig.PartitionKeyName].S,
                    t => DateTimeOffset.Parse(t[ChannelUsersConfig.Attributes.ChannelCreated].S));
        
        // TODO handle channelsResponse.UnprocessedKeys
        var channelsResponse = await _dynamoDbClient.BatchGetItemAsync(batchChannelsRequest, cancellationToken);
        var channels = channelsResponse.Responses[ChannelUsersConfig.TableName]
            .OrderBy(OrderSelector)
            .Select(t => t.ToChannel())
            .ToList();
        
        _memoryCache.SetUserChannels(userId, channels);
        
        return GetPageResult(channels, pager);

        DateTimeOffset OrderSelector(Dictionary<string, AttributeValue> t) => userSubscribedToChannelDate[t[ChannelUsersConfig.PartitionKeyName].S];
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
            TableName = ChannelUsersConfig.TableName,
            Key = Mapper.GetChannelUserKey(channelId, userId)
        };
        
        _memoryCache.ResetUserChannels(userId);
        _memoryCache.ResetChannelUserPhrases(channelId, userId);
        await _dynamoDbClient.DeleteItemAsync(deleteItemRequest, cancellationToken);
    }

    public async Task<UserChannelResponse> GetAllChannelUsersRelations(bool onlyWithPhrases, CancellationToken cancellationToken)
    {
        var attributesToProject = string.Join(
            ",", 
            [
                ChannelUsersConfig.PartitionKeyName,
                ChannelUsersConfig.SortKeyName,
                ChannelUsersConfig.Attributes.ChannelUserPhrases, 
                ChannelUsersConfig.Attributes.ChannelUserLastMessage
            ]);

        var filterExpression = $"begins_with({ChannelUsersConfig.PartitionKeyName}, :channel_prefix) AND begins_with({ChannelUsersConfig.SortKeyName}, :user_prefix)";
        var expressionAttributes = new Dictionary<string, AttributeValue>
        {
            [":channel_prefix"] = new() {S = Mapper.ChannelIdPrefix},
            [":user_prefix"] = new() {S = Mapper.UserIdPrefix},
        };

        if (onlyWithPhrases)
        {
            filterExpression += $" AND attribute_exists({ChannelUsersConfig.Attributes.ChannelUserPhrases}) AND size({ChannelUsersConfig.Attributes.ChannelUserPhrases}) > :zero";
            expressionAttributes.Add(":zero", new AttributeValue{ N = "0" });
        }
        
        var scanRequest = new ScanRequest
        {
            TableName = ChannelUsersConfig.TableName,
            FilterExpression = filterExpression,
            ExpressionAttributeValues = expressionAttributes,
            Select = "SPECIFIC_ATTRIBUTES",
            ProjectionExpression = attributesToProject
        };
        
        var channelUsersResponse = await _dynamoDbClient.ScanAsync(scanRequest, cancellationToken);
        if (channelUsersResponse.Items.Any() is false)
        {
            return UserChannelResponse.Empty;
        }

        var channels = new Dictionary<long, Channel>();
        var getBatchItemsKeys = new List<Dictionary<string, AttributeValue>>();
        foreach (var item in channelUsersResponse.Items)
        {
            var channelId = Mapper.ParseChannelKey(item[ChannelUsersConfig.PartitionKeyName].S);
            if (_memoryCache.GetChannel(channelId) is { } channel)
            {
                channels.Add(channelId, channel);
            }
            else
            {
                getBatchItemsKeys.Add(Mapper.GetChannelKey(channelId));
            }
        }

        if (getBatchItemsKeys.Any())
        {
            var batchGetItemRequest = new BatchGetItemRequest
            {
                RequestItems = new Dictionary<string, KeysAndAttributes>
                {
                    [ChannelUsersConfig.TableName] = new KeysAndAttributes()
                    {
                        Keys = getBatchItemsKeys
                    }
                }
            };
            
            var channelsResponse = await  _dynamoDbClient.BatchGetItemAsync(batchGetItemRequest, cancellationToken);
            foreach (var channel in channelsResponse
                         .Responses[ChannelUsersConfig.TableName]
                         .Select(channelAttributes => channelAttributes.ToChannel()))
            {
                channels.Add(channel.ChannelId, channel);
            }
        }

        var items = channelUsersResponse.Items
            .Select(t =>
            {
                var channelUser = t.ToChannelUser();
                var channel = channels[channelUser.ChannelId];
                return new UserChannelItemExtended
                {
                    UserId = channelUser.UserId,
                    Phrases = channelUser.Phrases,
                    LastMessage = channelUser.LastMessage ?? 0,
                    Channel = channel
                };
            })
            .ToList();

        return new UserChannelResponse(items);
    }

    public async Task UpdateLastMessage(long channelId, long userId, long lastMessage, CancellationToken cancellationToken)
    {
        var updateItemRequest = new UpdateItemRequest
        {
            TableName = ChannelUsersConfig.TableName,
            Key = Mapper.GetChannelUserKey(channelId, userId),
            UpdateExpression = "SET #lastMessage = :value",
            ConditionExpression = "attribute_exists(#partitionKey) AND attribute_exists(#sortKey)",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                ["#lastMessage"] = ChannelUsersConfig.Attributes.ChannelUserLastMessage,
                ["#partitionKey"] = ChannelUsersConfig.PartitionKeyName,
                ["#sortKey"] = ChannelUsersConfig.SortKeyName
            },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>{ [":value"] = new() { N = lastMessage.ToString()}},
        };
        
        await _dynamoDbClient.UpdateItemAsync(updateItemRequest, cancellationToken);
    }

    private async Task SetNewPhrases(ChannelUser channelUser, List<string> newPhrases, CancellationToken cancellationToken)
    {
        UpdateItemRequest updateItemRequest;
        if (newPhrases.Count == 0)
        {
            updateItemRequest = new UpdateItemRequest
            {
                TableName = ChannelUsersConfig.TableName, 
                Key = Mapper.GetChannelUserKey(channelUser),
                ExpressionAttributeNames = new Dictionary<string, string> { { "#phrases", ChannelUsersConfig.Attributes.ChannelUserPhrases } },
                UpdateExpression = "REMOVE #phrases",
            };
        }
        else
        {
            updateItemRequest = new UpdateItemRequest
            {
                TableName = ChannelUsersConfig.TableName, 
                Key = Mapper.GetChannelUserKey(channelUser),
                ExpressionAttributeNames = new Dictionary<string, string> { { "#phrases", ChannelUsersConfig.Attributes.ChannelUserPhrases } },
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
            TableName = ChannelUsersConfig.TableName,
            Key = Mapper.GetChannelUserKey(channelId, userId),
        };
        
        var channelUserResult = await _dynamoDbClient.GetItemAsync(getChannelUserRequest, cancellationToken);
        return 
            channelUserResult.Item?.Count is > 0 
                ? channelUserResult.Item.ToChannelUser() 
                : null;
    }
    
    private async Task<bool> PutItemIfNotExists(Dictionary<string, AttributeValue> item, CancellationToken cancellationToken)
    {
        var channelRequest = new PutItemRequest
        {
            TableName = ChannelUsersConfig.TableName,
            Item = item,
            ConditionExpression = $"attribute_not_exists({ChannelUsersConfig.PartitionKeyName}) AND attribute_not_exists({ChannelUsersConfig.SortKeyName})",
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
