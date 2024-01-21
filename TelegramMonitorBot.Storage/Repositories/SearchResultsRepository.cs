using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using TelegramMonitorBot.Domain.Models;
using TelegramMonitorBot.Storage.Exceptions;
using TelegramMonitorBot.Storage.Mapping;
using TelegramMonitorBot.Storage.Repositories.Abstractions;

using SearchResultsConfig = TelegramMonitorBot.Storage.DynamoDbConfig.SearchResults;
using Mapper = TelegramMonitorBot.Storage.Mapping.SearchResultsMapping;

namespace TelegramMonitorBot.Storage.Repositories;

public class SearchResultsRepository : ISearchResultsRepository
{
    private readonly AmazonDynamoDBClient _dynamoDbClient;

    private static readonly TimeSpan DefaultTimeToLive = TimeSpan.FromDays(3);

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
            foreach (var message in newItem.Value.Where(t => existingMessagesIds.Contains(t.Id) is false))
            {
                existingMessages.Add(message);
                hasChanges = true;
            }
        }

        if (hasChanges is false)
        {
            await UpdateLastMessage(searchResults.ChannelId, searchResults.UserId, lastMessage, cancellationToken);
            return;
        }

        try
        {
            await UpdateSearchResultsAndLastMessage(lastMessage, existingSearch, cancellationToken);
        }
        catch (ConcurrentUpdateException)
        {
            // ignore
            // If concurrent conflict happen we should wait until next search
        }
    }

    private async Task UpdateSearchResultsAndLastMessage(long lastMessage, SearchResults searchResults, CancellationToken cancellationToken)
    {
        var updateLastMessageRequest = GetUpdateLastMessageRequest(searchResults.ChannelId, searchResults.UserId, lastMessage);
        var searchResultsItem = searchResults.ToDictionary(DefaultTimeToLive);
        
        (string Name, AttributeValue Value) 
            expiredAtAttribute = (SearchResultsConfig.Attributes.ExpiredAt, searchResultsItem[SearchResultsConfig.Attributes.ExpiredAt]), 
            versionAttribute = (SearchResultsConfig.Attributes.VersionNumber, searchResultsItem[SearchResultsConfig.Attributes.VersionNumber]),
            searchResultsAttribute = (SearchResultsConfig.Attributes.SearchResults, searchResultsItem[SearchResultsConfig.Attributes.SearchResults]);
        
        var transactionRequest = new TransactWriteItemsRequest
        {
            TransactItems =
            [
                new TransactWriteItem
                {
                    Update = new Update
                    {
                        TableName = SearchResultsConfig.TableName,
                        Key = Mapper.GetSearchResultsKey(searchResults.ChannelId, searchResults.UserId),
                        UpdateExpression = "SET #searchResults = :searchResults, #version = :newVersion, #expiredAt = :expiredAt",
                        ConditionExpression = "#version = :oldVersion",
                        ExpressionAttributeNames = new Dictionary<string, string>
                        {
                            ["#searchResults"] = searchResultsAttribute.Name,
                            ["#version"] = versionAttribute.Name,
                            ["#expiredAt"] = expiredAtAttribute.Name,
                        },
                        ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                        {
                            [":searchResults"] = searchResultsAttribute.Value,
                            [":expiredAt"] = expiredAtAttribute.Value,
                            [":oldVersion"] = versionAttribute.Value,
                            [":newVersion"] = new () { N = (searchResults.Version + 1).ToString()}
                        }
                    },
                },
                new TransactWriteItem
                {
                    Update = new Update
                    {
                        TableName = updateLastMessageRequest.TableName,
                        Key = updateLastMessageRequest.Key,
                        UpdateExpression = updateLastMessageRequest.UpdateExpression,
                        ConditionExpression = updateLastMessageRequest.ConditionExpression,
                        ExpressionAttributeNames = updateLastMessageRequest.ExpressionAttributeNames,
                        ExpressionAttributeValues = updateLastMessageRequest.ExpressionAttributeValues,
                    }
                }
            ]
        };

        try
        {
            await _dynamoDbClient.TransactWriteItemsAsync(transactionRequest, cancellationToken);
        }
        catch (TransactionCanceledException ex) when(ex.CancellationReasons.FirstOrDefault(t => t.Code == "ConditionalCheckFailed") is not null)
        {
            throw new ConcurrentUpdateException();
        }
    }
    
    private async Task PutSearchResultsAndUpdateLastMessage(long lastMessage, SearchResults searchResults, CancellationToken cancellationToken)
    {
        var updateLastMessageRequest = GetUpdateLastMessageRequest(searchResults.ChannelId, searchResults.UserId, lastMessage);
        var transactionRequest = new TransactWriteItemsRequest
        {
            TransactItems =
            [
                new TransactWriteItem
                {
                    Put = new Put
                    {
                        TableName = SearchResultsConfig.TableName,
                        Item = searchResults.ToDictionary(DefaultTimeToLive),
                        
                    },
                },
                new TransactWriteItem
                {
                    Update = new Update
                    {
                        TableName = updateLastMessageRequest.TableName,
                        Key = updateLastMessageRequest.Key,
                        UpdateExpression = updateLastMessageRequest.UpdateExpression,
                        ConditionExpression = updateLastMessageRequest.ConditionExpression,
                        ExpressionAttributeNames = updateLastMessageRequest.ExpressionAttributeNames,
                        ExpressionAttributeValues = updateLastMessageRequest.ExpressionAttributeValues,
                    }
                }
            ]
        };

        await _dynamoDbClient.TransactWriteItemsAsync(transactionRequest, cancellationToken);
    }

    private async Task<SearchResults?> GetSearchResults(long channelId, long userId, CancellationToken cancellationToken)
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

    private async Task UpdateLastMessage(long channelId, long userId, long lastMessage, CancellationToken cancellationToken)
    {
        var updateLastMessageRequest = GetUpdateLastMessageRequest(channelId, userId, lastMessage);
    
        var updateItemRequest = new UpdateItemRequest
        {
            TableName = updateLastMessageRequest.TableName,
            Key = updateLastMessageRequest.Key,
            UpdateExpression = updateLastMessageRequest.UpdateExpression,
            ConditionExpression = updateLastMessageRequest.ConditionExpression,
            ExpressionAttributeNames = updateLastMessageRequest.ExpressionAttributeNames,
            ExpressionAttributeValues = updateLastMessageRequest.ExpressionAttributeValues,
        };

        await _dynamoDbClient.UpdateItemAsync(updateItemRequest, cancellationToken);
    }

    private static UpdateLastMessageRequest GetUpdateLastMessageRequest(long channelId, long userId, long lastMessage)
    {
        const string tableName = DynamoDbConfig.ChannelUsers.TableName;
        var key = ChannelUsersMapper.GetChannelUserKey(channelId, userId);
        const string updateExpression = "SET #lastMessage = :value";
        const string conditionExpression = "attribute_exists(#partitionKey) AND attribute_exists(#sortKey) AND (attribute_not_exists(lastMessage) OR #lastMessage <= :value)";
        var expressionAttributeNames = new Dictionary<string, string>
        {
            ["#lastMessage"] = DynamoDbConfig.ChannelUsers.Attributes.ChannelUserLastMessage,
            ["#partitionKey"] = DynamoDbConfig.ChannelUsers.PartitionKeyName,
            ["#sortKey"] = DynamoDbConfig.ChannelUsers.SortKeyName
        };
        var expressionAttributeValues = new Dictionary<string, AttributeValue>
        {
            [":value"] = new() {N = lastMessage.ToString()}
        };
            
        var request = new UpdateLastMessageRequest(
            tableName,
            key,
            updateExpression,
            conditionExpression,
            expressionAttributeNames,
            expressionAttributeValues);

        return request;
    }

    private record UpdateLastMessageRequest(
        string TableName, 
        Dictionary<string, AttributeValue> Key,
        string UpdateExpression,
        string ConditionExpression,
        Dictionary<string, string> ExpressionAttributeNames,
        Dictionary<string, AttributeValue> ExpressionAttributeValues);
}
    