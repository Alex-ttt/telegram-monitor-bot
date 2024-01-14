using TelegramMonitorBot.Domain.Models;
using TelegramMonitorBot.Storage.Repositories.Abstractions.Models;
using TelegramMonitorBot.Storage.Repositories.Models;

namespace TelegramMonitorBot.Storage.Repositories.Abstractions;

/// <summary>
/// Interface defining repository methods for managing Users, Channels and Channel-User relationships.
/// </summary>
public interface IChannelUserRepository
{
    /// <summary>
    /// Checks if a channel-user relationship exists.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>True if the channel-user relationship exists, otherwise false.</returns>
    Task<bool> CheckChannelWithUser(long channelId, long userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds a user to a channel and vice versa, creating a channel-user relationship.
    /// </summary>
    /// <param name="user">The user to add to the channel.</param>
    /// <param name="channel">The channel to add the user to.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>True if the operation is successful, otherwise false.</returns>
    Task<bool> PutUserChannel(User user, Channel channel, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves a paginated list of channels subscribed by a user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="pager">Pager for pagination configuration.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A page result containing the list of channels subscribed by the user.</returns>
    Task<PageResult<Channel>> GetChannels(long userId, Pager? pager, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a channel by its ID.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The retrieved channel, or null if not found.</returns>
    Task<Channel?> GetChannel(long channelId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds phrases to a channel-user relationship.
    /// </summary>
    /// <param name="channelUser">The channel-user relationship to add phrases to.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    Task AddPhrases(ChannelUser channelUser, CancellationToken cancellationToken);
    
    /// <summary>
    /// Retrieves phrases associated with a specific channel-user relationship.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A collection of phrases associated with the channel-user relationship.</returns>
    Task<ICollection<string>> GetChannelUserPhrases(long channelId, long userId, CancellationToken cancellationToken);
    
    /// <summary>
    /// Removes a specific phrase from a channel-user relationship.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="phrase">The phrase to remove.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    Task RemovePhrase(long channelId, long userId, string phrase, CancellationToken cancellationToken);
    
    /// <summary>
    /// Removes a channel-user relationship.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    Task RemoveChannelUser(long channelId, long userId, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves all channel-user relationships along with associated user and channel details, optionally filtering by those with non-empty phrases.
    /// </summary>
    /// <param name="onlyWithPhrases">A boolean flag indicating whether to include only relationships with non-empty phrases.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A response containing a list of user-channel relationships with extended information.</returns>
    Task<UserChannelResponse> GetAllChannelUsersRelations(bool onlyWithPhrases, CancellationToken cancellationToken);
}
