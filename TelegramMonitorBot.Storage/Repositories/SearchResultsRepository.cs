using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using TelegramMonitorBot.Domain.Models;
using TelegramMonitorBot.Storage.Mapping;
using TelegramMonitorBot.Storage.Repositories.Abstractions;

using SearchResultsConfig = TelegramMonitorBot.Storage.DynamoDbConfig.SearchResults;
using Mapper = TelegramMonitorBot.Storage.Mapping.SearchResultsMapping;

namespace TelegramMonitorBot.Storage.Repositories;

public class SearchResultsRepository : ISearchResultsRepository
{
    private readonly AmazonDynamoDBClient _dynamoDbClient;

    private static readonly TimeSpan DefaultTimeToLive = TimeSpan.FromDays(7);

    public SearchResultsRepository(DynamoClientFactory clientFactory)
    {
        _dynamoDbClient = clientFactory.GetClient();
    }

    // TODO Merge with existing
    public async Task AddSearchResults(SearchResults searchResults, CancellationToken cancellationToken)
    {
        var putItemRequest = new PutItemRequest()
        {
            TableName = SearchResultsConfig.TableName,
            Item = searchResults.ToDictionary(DefaultTimeToLive)
        };
        
        await  _dynamoDbClient.PutItemAsync(putItemRequest, cancellationToken);
    }
}