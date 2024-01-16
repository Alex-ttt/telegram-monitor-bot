using MediatR;
using Microsoft.Extensions.Logging;
using TelegramMonitorBot.Domain.Models;
using TelegramMonitorBot.Storage.Repositories.Abstractions;
using TelegramMonitorBot.Storage.Repositories.Models;
using TelegramMonitorBot.TelegramApiClient;
using TelegramMonitorBot.TelegramApiClient.Models;
using Channel = TelegramMonitorBot.Domain.Models.Channel;

using SearchResultInfo = (long LastMessage, long UserId, TelegramMonitorBot.Domain.Models.SearchResults? SearchResults);

namespace TelegramMonitorBot.TelegramBotClient.Application.PhrasesSearch.Commands.SearchPhrases;

public class SearchPhrasesRequestHandler : IRequestHandler<SearchPhrasesRequest>
{
    private readonly ITelegramApiClient _telegramApiClient;
    private readonly IChannelUserRepository _channelUserRepository;
    private readonly ISearchResultsRepository _searchResultsRepository;
    private readonly ILogger<SearchPhrasesRequestHandler> _logger;
    
    
    public SearchPhrasesRequestHandler(
        ITelegramApiClient telegramApiClient, 
        IChannelUserRepository channelUserRepository,
        ISearchResultsRepository searchResultsRepository,
        ILogger<SearchPhrasesRequestHandler> logger)
    {
        _telegramApiClient = telegramApiClient;
        _channelUserRepository = channelUserRepository;
        _searchResultsRepository = searchResultsRepository;
        _logger = logger;
    }

    public async Task Handle(SearchPhrasesRequest request, CancellationToken cancellationToken)
    {
        var channelsResponse = await _channelUserRepository.GetAllChannelUsersRelations(true, cancellationToken);
        if(channelsResponse.Items.Count is 0)
        {
            return;
        }

        var itemsByChannel = channelsResponse.Items
            .Where(t => t.Phrases?.Count > 0)
            .GroupBy(t => t.Channel);
        
        foreach (var itemsGroup in itemsByChannel)
        {
            var currentChannel = itemsGroup.Key;
            await foreach (var resultInfo in SearchByChannel(currentChannel, itemsGroup.AsEnumerable()).WithCancellation(cancellationToken))
            {
                if(resultInfo.SearchResults is { Results.Count: > 0 } searchResults)
                {
                    await _searchResultsRepository.MergeSearchResults(resultInfo.LastMessage, searchResults, cancellationToken);
                }
                else
                {
                    await _channelUserRepository.UpdateLastMessage(currentChannel.ChannelId, resultInfo.UserId, resultInfo.LastMessage, cancellationToken);
                }
            }
        }
    }
    
    private async IAsyncEnumerable<SearchResultInfo> SearchByChannel(
        Channel channel, 
        IEnumerable<UserChannelItemExtended> searchData)
    {
        // Wee need to load a channel once to operate with channelId
        var channelLoaded = await LoadChannel(channel);
        if (channelLoaded is false)
        {
            yield break;
        }

        foreach (var searchItem in searchData.Where(t => t.Phrases is {Count: > 0}))
        {
            var searchMessages = await _telegramApiClient.SearchMessages(searchItem.Channel.ChannelId, searchItem.Phrases!, searchItem.LastMessage);
            if (searchMessages is not {PhraseSearchMessages.Count: > 0})
            {
                yield return (searchMessages.LastMessage, searchItem.UserId, null);
                continue;
            }

            var results = searchMessages.PhraseSearchMessages
                .ToDictionary(t => t.Key, ConvertMessages);
            
            var searchResult = new SearchResults(
                channel.ChannelId,
                searchItem.UserId,
                results);
                
            yield return (searchMessages.LastMessage, searchItem.UserId, searchResult);
        }

        yield break;

        static IList<Message> ConvertMessages(KeyValuePair<string, ICollection<SearchMessage>> phrasesToSearchMessages)
        {
            var newMessages = phrasesToSearchMessages.Value
                .Select(m => new Message(m.Id, m.Link, m.Date))
                .ToList();
            
            return newMessages;
        }
    }

    private async Task<bool> LoadChannel(Channel currentChannel)
    {
        // Wee need to load a channel once to operate with channelId
        var loadedChannel = await _telegramApiClient.FindChannelByName(currentChannel.Name);
        if(loadedChannel is null)
        {
            _logger.LogWarning("Channel \"{ChannelName}\" not found", currentChannel.Name);
            return false;
        }
        
        if (loadedChannel.Id != currentChannel.ChannelId)
        {
            _logger.LogWarning(
                "Channel \"{ChannelName}\" identifier has been changed from {OldId} to {NewId}", 
                currentChannel.Name, 
                currentChannel.ChannelId, 
                loadedChannel.Id);
            
            return false;
        }

        return true;
    }
}