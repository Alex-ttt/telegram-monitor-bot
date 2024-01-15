using Amazon.DynamoDBv2.Model;
using TelegramMonitorBot.Domain.Models;
using TelegramMonitorBot.Storage.Extensions;
using ChannelUsersConfig = TelegramMonitorBot.Storage.DynamoDbConfig.ChannelUsers;

namespace TelegramMonitorBot.Storage.Mapping;

internal static class ChannelUsersMapper
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
            [ChannelUsersConfig.PartitionKeyName] = new() {S = key},
            [ChannelUsersConfig.SortKeyName] = new() {S = key},
            [ChannelUsersConfig.Attributes.UserName] = new() {S = user.Name},
            [ChannelUsersConfig.Attributes.UserCreated] = new() {S = user.Created.ToISO_8601()},
        };
    }
    
    internal static Dictionary<string, AttributeValue> ToDictionary(this Channel channel)
    {
        var key = ChannelIdToKeyValue(channel.ChannelId);
        
        return new Dictionary<string, AttributeValue>
        {
            [ChannelUsersConfig.PartitionKeyName] = new () { S = key},
            [ChannelUsersConfig.SortKeyName] = new () { S = key},
            [ChannelUsersConfig.Attributes.ChannelName] = new() { S = channel.Name},
            [ChannelUsersConfig.Attributes.ChannelCreated] = new() {S = channel.Created.ToISO_8601()},
        };
    }
    
    internal static Dictionary<string, AttributeValue> ToDictionary(this ChannelUser channelUser)
    {
        var result = new Dictionary<string, AttributeValue>
        {
            [ChannelUsersConfig.PartitionKeyName] = new() { S = ChannelIdToKeyValue(channelUser.ChannelId)},
            [ChannelUsersConfig.SortKeyName] = new() { S = UserIdToKeyValue(channelUser.UserId)},
            [ChannelUsersConfig.Attributes.ChannelUserCreated] = new() {S = channelUser.Created.ToISO_8601()},
        };

        if (channelUser.Phrases is { Count: > 0} phrases)
        {
            result.Add(ChannelUsersConfig.Attributes.ChannelUserPhrases, new AttributeValue { SS = phrases});
        }
        
        if(channelUser.LastMessage is { } lastMessage)
        {
            result.Add(ChannelUsersConfig.Attributes.ChannelUserLastMessage, new AttributeValue { N = lastMessage.ToString()});
        }

        return result;
    }

    internal static Channel ToChannel(this Dictionary<string, AttributeValue> dictionary)
    {
        var channelId = ParseChannelKey(dictionary[ChannelUsersConfig.PartitionKeyName].S);
        
        var name = dictionary[ChannelUsersConfig.Attributes.ChannelName].S;
        var created = DateTimeOffset.Parse(dictionary[ChannelUsersConfig.Attributes.ChannelCreated].S);
        
        return new Channel(channelId, name)
        {
            Created = created,
        }; 
    }

    internal static ChannelUser ToChannelUser(this Dictionary<string, AttributeValue> dictionary)
    {
        var channelId = ParseChannelKey(dictionary[ChannelUsersConfig.PartitionKeyName].S);
        
        var userIdKey = dictionary[ChannelUsersConfig.SortKeyName].S;
        var userId = long.Parse(userIdKey[UserIdPrefix.Length..]);

        List<string>? phrases = null;
        if(dictionary.TryGetValue(ChannelUsersConfig.Attributes.ChannelUserPhrases, out var phrasesAttribute))
        {
            phrases = phrasesAttribute.SS;
        }
        
        long? lastMessage = null;
        if(dictionary.TryGetValue(ChannelUsersConfig.Attributes.ChannelUserLastMessage, out var lastMessageAttribute))
        {
            lastMessage = long.Parse(lastMessageAttribute.N);
        }

        return new ChannelUser(channelId, userId, phrases, lastMessage);
    }

    internal static Dictionary<string, AttributeValue> GetChannelUserKey(ChannelUser channelUser)
    {
        return GetChannelUserKey(channelUser.ChannelId, channelUser.UserId);
    }
    
    internal static Dictionary<string, AttributeValue> GetChannelUserKey(long channelId, long userId)
    {
        return new Dictionary<string, AttributeValue>
        {
            [ChannelUsersConfig.PartitionKeyName] = new() { S = ChannelIdToKeyValue(channelId) },
            [ChannelUsersConfig.SortKeyName] = new() { S = UserIdToKeyValue(userId) },
        };
    }

    internal static Dictionary<string, AttributeValue> GetChannelKey(long channelId)
    {
        return new Dictionary<string, AttributeValue>
        {
            [ChannelUsersConfig.PartitionKeyName] = new() { S = ChannelIdToKeyValue(channelId) },
            [ChannelUsersConfig.SortKeyName] = new() { S = ChannelIdToKeyValue(channelId) },
        }; 
    }

    internal static long ParseChannelKey(string channelKey)
    {
        var channelId = long.Parse(channelKey[ChannelIdPrefix.Length..]);

        return channelId;
    }
}