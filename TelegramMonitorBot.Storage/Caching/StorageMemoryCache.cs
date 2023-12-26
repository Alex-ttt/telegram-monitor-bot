using Microsoft.Extensions.Caching.Memory;
using TelegramMonitorBot.Domain.Models;

namespace TelegramMonitorBot.Storage.Caching;

internal class StorageMemoryCache 
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    public void SetUserChannels(long userId, ICollection<Channel> channels)
    {
        _cache.Set(UserChannelsKey(userId), channels, TimeSpan.FromMinutes(5));
    }

    public ICollection<Channel>? GetUserChannels(long userId)
    {
        return _cache.Get<ICollection<Channel>>(UserChannelsKey(userId));
    }

    public void ResetUserChannels(long userId)
    {
        _cache.Remove(UserChannelsKey(userId));
    }

    private static string UserChannelsKey(long userId) => $"user_channels_{userId}";
}
