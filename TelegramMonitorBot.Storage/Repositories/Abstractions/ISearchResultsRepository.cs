using TelegramMonitorBot.Domain.Models;

namespace TelegramMonitorBot.Storage.Repositories.Abstractions;

public interface ISearchResultsRepository
{
    Task MergeSearchResults(long lastMessage, SearchResults searchResults, CancellationToken cancellationToken);
}
