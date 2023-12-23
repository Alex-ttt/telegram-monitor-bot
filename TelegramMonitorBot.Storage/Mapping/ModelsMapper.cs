using Amazon.DynamoDBv2.Model;
using TelegramMonitorBot.Domain.Models;

namespace TelegramMonitorBot.Storage.Mapping;

internal static class ModelsMapper
{
    internal static Dictionary<string, AttributeValue> ToDictionary(this User user)
    {
        return new Dictionary<string, AttributeValue>
        {
            [nameof(User.UserId)] = new() {N = user.UserId.ToString()},
            [nameof(User.Name)] = new(user.Name),
        };
    }
    
    internal static Dictionary<string, AttributeValue> ToDictionary(this Channel channel)
    {
        return new Dictionary<string, AttributeValue>
        {
            [nameof(Channel.ChannelId)] = new () { N = channel.ChannelId.ToString()},
            [nameof(Channel.Name)] = new() { S = channel.Name},
        };
    }
    
    internal static Dictionary<string, AttributeValue> ToDictionary(this UserChannel userChannel)
    {
        var result = new Dictionary<string, AttributeValue>
        {
            [nameof(UserChannel.ChannelId)] = new() { N = userChannel.ChannelId.ToString()},
            [nameof(UserChannel.UserId)] = new() { N = userChannel.UserId.ToString()},
            [nameof(UserChannel.Created)] = new() {S = userChannel.Created.ToString()},
        };

        if (userChannel.Phrases?.Count is > 0)
        {
            result.Add(nameof(UserChannel.Phrases), new AttributeValue { SS = userChannel.Phrases});
        }

        return result;
    }

    internal static Channel ToChannel(this Dictionary<string, AttributeValue> dictionary)
    {
        var channelId = long.Parse(dictionary[nameof(Channel.ChannelId)].N);
        var name = dictionary[nameof(Channel.Name)].S;
        
        return new Channel(channelId, name); 
    }
    
}