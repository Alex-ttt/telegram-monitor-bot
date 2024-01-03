using Microsoft.Extensions.Caching.Memory;
using TelegramMonitorBot.Domain.Models;

namespace TelegramMonitorBot.Storage.Caching;

internal class StorageMemoryCache
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    private static readonly TimeSpan UserChannelsLifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ChannelLifetime = TimeSpan.FromMinutes(2.5);
    private static readonly TimeSpan ChannelUserPhrasesLifetime = TimeSpan.FromMinutes(1);
    
    public void SetUserChannels(long userId, ICollection<Channel> channels)
    {
        _cache.Set(UserChannelsKey(userId), channels, UserChannelsLifetime);
    }

    public ICollection<Channel>? GetUserChannels(long userId)
    {
        return _cache.Get<ICollection<Channel>>(UserChannelsKey(userId));
    }

    public void ResetUserChannels(long userId)
    {
        _cache.Remove(UserChannelsKey(userId));
    }

    public Channel? GetChannel(long channelId)
    {
        return _cache.Get<Channel>(ChannelKey(channelId));
    }

    public void SetChannel(Channel channel)
    {
        _cache.Set(ChannelKey(channel.ChannelId), channel, ChannelLifetime);
    }

    public void ResetChannel(long channelId)
    {
        _cache.Remove(ChannelKey(channelId));
    }

    public void SetChannelUserPhrases(long channelId, long userId, ICollection<string> phrases)
    {
        _cache.Set(ChannelUserPhrasesKey(channelId, userId), phrases, ChannelUserPhrasesLifetime);
    }

    public ICollection<string>? GetChannelUserPhrases(long channelId, long userId)
    {
        return _cache.Get<ICollection<string>>(ChannelUserPhrasesKey(channelId, userId));
    }

    public void ResetChannelUserPhrases(long channelId, long userId)
    {
        _cache.Remove(ChannelUserPhrasesKey(channelId, userId));
    }
    
    private static string UserChannelsKey(long userId) => $"user_channels_{userId}";
    private static string ChannelKey(long channelId) => $"channel_{channelId}";
    private static string  ChannelUserPhrasesKey(long channelId, long userId) => $"channel_user_phrases_{channelId}_{userId}";
}
