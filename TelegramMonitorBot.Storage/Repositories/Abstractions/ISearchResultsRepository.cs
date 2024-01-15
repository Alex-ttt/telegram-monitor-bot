using TelegramMonitorBot.Domain.Models;

namespace TelegramMonitorBot.Storage.Repositories.Abstractions;

public interface ISearchResultsRepository
{
    Task AddSearchResults(SearchResults searchResults, CancellationToken cancellationToken);
}