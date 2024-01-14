using Amazon.DynamoDBv2.Model;
using TelegramMonitorBot.Domain.Models;
using Attributes = TelegramMonitorBot.Storage.DynamoDbConfig.SearchResults.Attributes;
using SearchResultsConfig = TelegramMonitorBot.Storage.DynamoDbConfig.SearchResults;

namespace TelegramMonitorBot.Storage.Mapping;

internal static class SearchResultsMapping
{
    private const string ChannelIdPrefix = "channel#";
    private const string UserIdPrefix = "user#";
    
    internal static Dictionary<string, AttributeValue> ToDictionary(this SearchResults searchResults, TimeSpan ttl)
    {
        var unixTtl = (int)(DateTime.UtcNow.Add(ttl) - DateTime.UnixEpoch).TotalSeconds;
        var result = new Dictionary<string, AttributeValue>
        {
            [DynamoDbConfig.SearchResults.PartitionKeyName] = new()
            {
                S = ChannelIdToKeyValue(searchResults.ChannelId)
            },
            [DynamoDbConfig.SearchResults.SortKeyName] = new()
            {
                S = UserIdToKeyValue(searchResults.UserId)
            },
            [Attributes.SearchResults] = new()
            {
                L = searchResults.Results.Select(GetSearchResultAttribute).ToList(),
            },
            [Attributes.Ttl] = new()
            {
                N = unixTtl.ToString()
            }
        };

        return result;
    }

    internal static SearchResults ToSearchResults(this Dictionary<string, AttributeValue> itemAttributes)
    {
        var channelId = ParseChannelKey(itemAttributes[SearchResultsConfig.PartitionKeyName].S);
        var userId = ParseUserKey(itemAttributes[SearchResultsConfig.SortKeyName].S);

        if (itemAttributes[Attributes.SearchResults].L is not {Count: > 0} searchResultsItem)
        {
            return new SearchResults(channelId, userId, Array.Empty<SearchResult>());
        }

        var resultsCollection = searchResultsItem
            .Select(t => ToSearchResult(t.M))
            .ToList();

        var searchResults = new SearchResults(channelId, userId, resultsCollection);

        return searchResults;
    }

    private static SearchResult ToSearchResult(this Dictionary<string, AttributeValue> itemAttributes)
    {
        var phrase = itemAttributes[Attributes.SearchResultsPhrase].S;
        var messages = itemAttributes[Attributes.SearchResultsMessages].L
            .Select(t => ToMessage(t.M))
            .ToList();

        return new SearchResult(phrase, messages);
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
                [Attributes.SearchResultsMessageDate] = new() { S = message.Date.ToString()},
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
    
    private static AttributeValue GetSearchResultAttribute(SearchResult searchResult)
    {
        return new AttributeValue
        {
            M = new Dictionary<string, AttributeValue>
            {
                [Attributes.SearchResultsPhrase] = new() { S = searchResult.Phrase},
                [Attributes.SearchResultsMessages] = GetMessagesAttribute(searchResult.Messages)
            }
        };
    }

    private static string UserIdToKeyValue(long userId) => $"{UserIdPrefix}{userId}";

    private static string ChannelIdToKeyValue(long channelId) => $"{ChannelIdPrefix}{channelId}";
    
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
}