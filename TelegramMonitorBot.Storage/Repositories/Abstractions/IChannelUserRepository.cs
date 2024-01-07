using TelegramMonitorBot.Domain.Models;
using TelegramMonitorBot.Storage.Repositories.Abstractions.Models;

namespace TelegramMonitorBot.Storage.Repositories.Abstractions;

public interface IChannelUserRepository
{
    Task<bool> CheckChannelWithUser(long channelId, long userId, CancellationToken cancellationToken = default);
    Task<bool> PutUserChannel(User user, Channel channel, CancellationToken cancellationToken = default);

    Task<PageResult<Channel>> GetChannels(long userId, Pager? pager, CancellationToken cancellationToken = default);
    Task<Channel?> GetChannel(long channelId, CancellationToken cancellationToken = default);
    Task AddPhrases(ChannelUser channelUser, CancellationToken cancellationToken);
    Task<ICollection<string>> GetChannelUserPhrases(long channelId, long userId, CancellationToken cancellationToken);
    Task RemovePhrase(long channelId, long userId, string phrase, CancellationToken cancellationToken);
    Task RemoveChannelUser(long channelId, long userId, CancellationToken cancellationToken);
}
