using Amazon.DynamoDBv2.Model;
using TelegramMonitorBot.Domain.Models;
using TelegramMonitorBot.Storage.Extensions;

using Attributes = TelegramMonitorBot.Storage.DynamoDbConfig.SearchResults.Attributes;
using SearchResultsConfig = TelegramMonitorBot.Storage.DynamoDbConfig.SearchResults;

namespace TelegramMonitorBot.Storage.Mapping;

internal static class SearchResultsMapping
{
    private const string ChannelIdPrefix = "channel#";
    private const string UserIdPrefix = "user#";
    
    internal static Dictionary<string, AttributeValue> ToDictionary(this SearchResults searchResults, TimeSpan timeToLive)
    {
        var unixTtl = (int)(DateTime.UtcNow.Add(timeToLive) - DateTime.UnixEpoch).TotalSeconds;
        var result = new Dictionary<string, AttributeValue>
        {
            [SearchResultsConfig.PartitionKeyName] = new() { S = ChannelIdToKeyValue(searchResults.ChannelId)},
            [SearchResultsConfig.SortKeyName] = new() { S = UserIdToKeyValue(searchResults.UserId)},
            [Attributes.ExpiredAt] = new() { N = unixTtl.ToString()},
            [Attributes.VersionNumber] = new() { N = searchResults.Version.ToString() },
        };

        if (searchResults.Results.Count != 0)
        {
            result.Add(
                Attributes.SearchResults, 
                new AttributeValue 
                {
                    L = searchResults.Results.Select(GetResultAttribute).ToList(),
                });
        }

        return result;
    }

    internal static Dictionary<string, AttributeValue> GetSearchResultsKey(SearchResults searchResults)
    {
        return GetSearchResultsKey(searchResults.ChannelId, searchResults.UserId);
    }
    
    internal static Dictionary<string, AttributeValue> GetSearchResultsKey(long channelId, long userId)
    {
        return new Dictionary<string, AttributeValue>
        {
            [SearchResultsConfig.PartitionKeyName] = new() { S = ChannelIdToKeyValue(channelId) },
            [SearchResultsConfig.SortKeyName] = new() { S = UserIdToKeyValue(userId) },
        };
    }

    internal static SearchResults ToSearchResults(this Dictionary<string, AttributeValue> itemAttributes)
    {
        var channelId = ParseChannelKey(itemAttributes[SearchResultsConfig.PartitionKeyName].S);
        var userId = ParseUserKey(itemAttributes[SearchResultsConfig.SortKeyName].S);
        var version = int.Parse(itemAttributes[Attributes.VersionNumber].N);

        if (itemAttributes[Attributes.SearchResults].L is not {Count: > 0} searchResultsItem)
        {
            return SearchResults.GetEmpty(channelId, userId);
        }

        var resultsCollection = searchResultsItem
            .Select(t => ToSearchResult(t.M))
            .ToDictionary(t => t.Key, t => t.Value);

        var searchResults = new SearchResults(channelId, userId, resultsCollection, version);

        return searchResults;
    }
    
    internal static long ParseChannelKey(string channelKey)
    {
        var channelId = long.Parse(channelKey[ChannelIdPrefix.Length..]);

        return channelId;
    }
    
    internal static long ParseUserKey(string userKey)
    {
        var channelIdKey = userKey;
        var channelId = long.Parse(channelIdKey[UserIdPrefix.Length..]);

        return channelId;
    }
    
    private static KeyValuePair<string, IList<Message>> ToSearchResult(this Dictionary<string, AttributeValue> itemAttributes)
    {
        var phrase = itemAttributes[Attributes.SearchResultsPhrase].S;
        var messages = itemAttributes[Attributes.SearchResultsMessages].L
            .Select(t => ToMessage(t.M))
            .ToList();

        return new KeyValuePair<string, IList<Message>>(phrase, messages);
    }

    private static Message ToMessage(this Dictionary<string, AttributeValue> itemAttributes)
    {
        var messageId = long.Parse(itemAttributes[Attributes.SearchResultsMessageId].N);
        var messageLink = itemAttributes[Attributes.SearchResultsMessageLink].S;
        var date = DateTimeOffset.Parse(itemAttributes[Attributes.SearchResultsMessageDate].S);
        
        return new Message(messageId, messageLink, date);
    }
    
    private static AttributeValue GetMessageAttribute(Message message)
    {
        return new AttributeValue
        {
            M = new Dictionary<string, AttributeValue>
            {
                [Attributes.SearchResultsMessageId] = new() { N = message.Id.ToString()},
                [Attributes.SearchResultsMessageLink] = new() { S = message.Link},
                [Attributes.SearchResultsMessageDate] = new() { S = message.Date.ToISO_8601()},
            }
        };
    }
    
    private static AttributeValue GetMessagesAttribute(IEnumerable<Message> messages)
    {
        return new AttributeValue
        {
            L = messages.Select(GetMessageAttribute).ToList()
        };
    }
    
    private static AttributeValue GetResultAttribute(KeyValuePair<string, IList<Message>> searchResult)
    {
        return new AttributeValue
        {
            M = new Dictionary<string, AttributeValue>
            {
                [Attributes.SearchResultsPhrase] = new() { S = searchResult.Key},
                [Attributes.SearchResultsMessages] = GetMessagesAttribute(searchResult.Value)
            }
        };
    }

    private static string UserIdToKeyValue(long userId) => $"{UserIdPrefix}{userId}";

    private static string ChannelIdToKeyValue(long channelId) => $"{ChannelIdPrefix}{channelId}";
}