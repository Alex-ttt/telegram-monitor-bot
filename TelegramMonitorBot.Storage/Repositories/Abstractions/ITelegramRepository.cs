using TelegramMonitorBot.Domain.Models;

namespace TelegramMonitorBot.Storage.Repositories.Abstractions;

public interface ITelegramRepository
{
    Task PutUserChannel(User user, Channel channel, CancellationToken cancellationToken = default);

    Task<ICollection<Channel>> GetChannels(long userId, CancellationToken cancellationToken = default);
}