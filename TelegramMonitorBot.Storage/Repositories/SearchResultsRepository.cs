using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using TelegramMonitorBot.Domain.Models;
using TelegramMonitorBot.Storage.Mapping;
using TelegramMonitorBot.Storage.Repositories.Abstractions;

using SearchResultsConfig = TelegramMonitorBot.Storage.DynamoDbConfig.SearchResults;
using Mapper = TelegramMonitorBot.Storage.Mapping.SearchResultsMapping;

namespace TelegramMonitorBot.Storage.Repositories;

public class SearchResultsRepository : ISearchResultsRepository
{
    private readonly AmazonDynamoDBClient _dynamoDbClient;

    private static readonly TimeSpan DefaultTimeToLive = TimeSpan.FromDays(7);

    public SearchResultsRepository(DynamoClientFactory clientFactory)
    {
        _dynamoDbClient = clientFactory.GetClient();
    }
    
    public async Task MergeSearchResults(long lastMessage, SearchResults searchResults, CancellationToken cancellationToken)
    {
        if (searchResults.Results.Count == 0)
        {
            await UpdateLastMessage(searchResults.ChannelId, searchResults.UserId, lastMessage, cancellationToken);
            return;
        }

        var existingSearch = await GetSearchResults(searchResults.ChannelId, searchResults.UserId, cancellationToken);
        if (existingSearch is null)
        {
            await PutSearchResultsAndUpdateLastMessage(lastMessage, searchResults, cancellationToken);
            return;
        }

        var hasChanges = false;
        foreach (var newItem in searchResults.Results)
        {
            if (existingSearch.Results.TryGetValue(newItem.Key, out var existingMessages) is false)
            {
                existingSearch.Results.Add(newItem);
                hasChanges = true;
                continue;
            }

            var existingMessagesIds = existingMessages.Select(t => t.Id).ToHashSet();
            foreach (var message in newItem.Value)
            {
                if (existingMessagesIds.Contains(message.Id) is false)
                {
                    existingMessages.Add(message);
                    hasChanges = true;
                }
            }
        }

        if (hasChanges is false)
        {
            await UpdateLastMessage(searchResults.ChannelId, searchResults.UserId, lastMessage, cancellationToken);
            return;
        }
        
        await PutSearchResultsAndUpdateLastMessage(lastMessage, existingSearch, cancellationToken);

    }

    private async Task PutSearchResultsAndUpdateLastMessage(long lastMessage, SearchResults searchResults,
        CancellationToken cancellationToken)
    {
        var transactionRequest = new TransactWriteItemsRequest
        {
            TransactItems =
            [
                new TransactWriteItem
                {
                    Put = new Put
                    {
                        TableName = SearchResultsConfig.TableName,
                        Item = searchResults.ToDictionary(DefaultTimeToLive)
                    },
                },
                new TransactWriteItem
                {
                    Update = new Update
                    {
                        TableName = DynamoDbConfig.ChannelUsers.TableName,
                        Key = ChannelUsersMapper.GetChannelUserKey(searchResults.ChannelId, searchResults.UserId),
                        UpdateExpression = "SET #lastMessage = :value",
                        ConditionExpression = "attribute_exists(#partitionKey) AND attribute_exists(#sortKey)",
                        ExpressionAttributeNames = new Dictionary<string, string>
                        {
                            ["#lastMessage"] = DynamoDbConfig.ChannelUsers.Attributes.ChannelUserLastMessage,
                            ["#partitionKey"] = DynamoDbConfig.ChannelUsers.PartitionKeyName,
                            ["#sortKey"] = DynamoDbConfig.ChannelUsers.SortKeyName
                        },
                        ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                            {[":value"] = new() {N = lastMessage.ToString()}},
                    }
                }
            ]
        };

        await _dynamoDbClient.TransactWriteItemsAsync(transactionRequest, cancellationToken);
    }

    private async Task<SearchResults?> GetSearchResults(long channelId, long userId,
        CancellationToken cancellationToken)
    {
        var getItemRequest = new GetItemRequest
        {
            TableName = SearchResultsConfig.TableName,
            Key = Mapper.GetSearchResultsKey(channelId, userId)
        };

        var getItemResponse = await _dynamoDbClient.GetItemAsync(getItemRequest, cancellationToken);
        var searchResults = getItemResponse.Item is {Count: > 0} item
            ? item.ToSearchResults()
            : null;

        return searchResults;
    }

    private async Task UpdateLastMessage(long channelId, long userId, long lastMessage,
        CancellationToken cancellationToken)
    {
        var updateItemRequest = new UpdateItemRequest
        {
            TableName = DynamoDbConfig.ChannelUsers.TableName,
            Key = ChannelUsersMapper.GetChannelUserKey(channelId, userId),
            UpdateExpression = "SET #lastMessage = :value",
            ConditionExpression = "attribute_exists(#partitionKey) AND attribute_exists(#sortKey)",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                ["#lastMessage"] = DynamoDbConfig.ChannelUsers.Attributes.ChannelUserLastMessage,
                ["#partitionKey"] = DynamoDbConfig.ChannelUsers.PartitionKeyName,
                ["#sortKey"] = DynamoDbConfig.ChannelUsers.SortKeyName
            },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {[":value"] = new() {N = lastMessage.ToString()}},
        };

        await _dynamoDbClient.UpdateItemAsync(updateItemRequest, cancellationToken);

    }
}