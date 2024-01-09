using Amazon.DynamoDBv2.Model;
using TelegramMonitorBot.Domain.Models;

using static  TelegramMonitorBot.Storage.DynamoDbConfig;

namespace TelegramMonitorBot.Storage.Mapping;

internal static class Mapper
{
    internal const string ChannelIdPrefix = "channel#";
    internal const string UserIdPrefix = "user#";
    
    internal static string UserIdToKeyValue(long userId) => $"{UserIdPrefix}{userId}";
    
    internal static string ChannelIdToKeyValue(long channelId) => $"{ChannelIdPrefix}{channelId}";
    
    internal static Dictionary<string, AttributeValue> ToDictionary(this User user)
    {
        var key = UserIdToKeyValue(user.UserId);
        
        return new Dictionary<string, AttributeValue>
        {
            [PartitionKeyName] = new() {S = key},
            [SortKeyName] = new() {S = key},
            [Attributes.UserName] = new() {S = user.Name},
            [Attributes.UserCreated] = new() {S = user.Created.ToString()},
        };
    }
    
    internal static Dictionary<string, AttributeValue> ToDictionary(this Channel channel)
    {
        var key = ChannelIdToKeyValue(channel.ChannelId);
        
        return new Dictionary<string, AttributeValue>
        {
            [PartitionKeyName] = new () { S = key},
            [SortKeyName] = new () { S = key},
            [Attributes.ChannelName] = new() { S = channel.Name},
            [Attributes.ChannelCreated] = new() {S = channel.Created.ToString()},
        };
    }
    
    internal static Dictionary<string, AttributeValue> ToDictionary(this ChannelUser channelUser)
    {
        var result = new Dictionary<string, AttributeValue>
        {
            [PartitionKeyName] = new() { S = ChannelIdToKeyValue(channelUser.ChannelId)},
            [SortKeyName] = new() { S = UserIdToKeyValue(channelUser.UserId)},
            [Attributes.ChannelUserCreated] = new() {S = channelUser.Created.ToString()},
        };

        if (channelUser.Phrases?.Count is > 0)
        {
            result.Add(Attributes.ChannelUserPhrases, new AttributeValue { SS = channelUser.Phrases});
        }

        return result;
    }

    internal static Channel ToChannel(this Dictionary<string, AttributeValue> dictionary)
    {
        var channelIdKey = dictionary[PartitionKeyName].S;
        var channelId = long.Parse(channelIdKey[ChannelIdPrefix.Length..]);
        
        var name = dictionary[Attributes.ChannelName].S;
        var created = DateTimeOffset.Parse(dictionary[Attributes.ChannelCreated].S);
        
        return new Channel(channelId, name)
        {
            Created = created,
        }; 
    }

    internal static ChannelUser ToChannelUser(this Dictionary<string, AttributeValue> dictionary)
    {
        var channelIdKey = dictionary[PartitionKeyName].S;
        var channelId = long.Parse(channelIdKey[ChannelIdPrefix.Length..]);
        
        var userIdKey = dictionary[SortKeyName].S;
        var userId = long.Parse(userIdKey[UserIdPrefix.Length..]);

        List<string>? phrases = null;
        if(dictionary.TryGetValue(Attributes.ChannelUserPhrases, out var phrasesAttribute))
        {
            phrases = phrasesAttribute.SS;
        }

        return new ChannelUser(channelId, userId, phrases);
    }

    internal static Dictionary<string, AttributeValue> GetChannelUserKey(ChannelUser channelUser)
    {
        return GetChannelUserKey(channelUser.ChannelId, channelUser.UserId);
    }
    
    internal static Dictionary<string, AttributeValue> GetChannelUserKey(long channelId, long userId)
    {
        return new Dictionary<string, AttributeValue>
        {
            [PartitionKeyName] = new() { S = ChannelIdToKeyValue(channelId) },
            [SortKeyName] = new() { S = UserIdToKeyValue(userId) },
        };
    }

    internal static Dictionary<string, AttributeValue> GetChannelKey(long channelId)
    {
        return new Dictionary<string, AttributeValue>
        {
            [PartitionKeyName] = new() { S = ChannelIdToKeyValue(channelId) },
            [SortKeyName] = new() { S = ChannelIdToKeyValue(channelId) },
        }; 
    }
}