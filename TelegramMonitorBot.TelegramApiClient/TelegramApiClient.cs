using System.Diagnostics;
using TdLib;
using TelegramMonitorBot.TelegramApiClient.Models;
using TelegramMonitorBot.TelegramApiClient.Services;

namespace TelegramMonitorBot.TelegramApiClient;

internal class TelegramApiClient : ITelegramApiClient
{
    private readonly TdClient _tdClient;

    internal TelegramApiClient(TdClient tdClient)
    {
        _tdClient = tdClient;
    }

    public async Task<Channel?> FindChannelByName(string channel)
    {
        var channelName = ChannelService.ParseChannel(channel);

        TdApi.Chat? searchPublicChatResult;
        try
        {
            searchPublicChatResult = await _tdClient.SearchPublicChatAsync(channelName);
        }
        catch (TdException ex) when (ex.Message is "USERNAME_NOT_OCCUPIED" or "USERNAME_INVALID")
        {
            return null;
        }

        if (searchPublicChatResult is null)
        {
            return null;
        }

        return new Channel
        {
            Id = searchPublicChatResult.Id,
            Name = channelName
        };

    }

    public async Task<ChannelSearchMessages?> SearchMessages(string channelName, ICollection<string> phrases)
    {
        if (phrases is not {Count: > 0})
        {
            return null;
        }

        var channel = await FindChannelByName(channelName);
        if (channel is null)
        {
            return null;
        }
        
        var phraseSearchMessages = new Dictionary<string, ICollection<SearchMessage>>();
        var lastMessage = long.MinValue;
        foreach (var phrase in phrases)
        {
            var foundMessages = await _tdClient.SearchChatMessagesAsync(channel.Id, phrase, limit: 100);
            if(foundMessages.Messages.Length == 0)
            {
                continue;
            }

            var currentMessages = new List<SearchMessage>();
            phraseSearchMessages[phrase] = currentMessages;
            foreach (var foundMessage in foundMessages.Messages)
            {
                var messageLink = await _tdClient.GetMessageLinkAsync(channel.Id, foundMessage.Id);
                var messageDate = DateTimeOffset.FromUnixTimeSeconds(foundMessage.Date);
                
                currentMessages.Add(new SearchMessage(foundMessage.Id, messageLink.Link, messageDate));
            }
            
            lastMessage = Math.Max(lastMessage, foundMessages.NextFromMessageId);
        }

        return new ChannelSearchMessages(channel.Id, lastMessage, phraseSearchMessages);
    }
}
