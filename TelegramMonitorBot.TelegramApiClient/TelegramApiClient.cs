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
            Name = channelName.ToLower()
        };
    }

    public async Task<ChannelSearchMessages?> SearchMessages(long channelId, ICollection<string> phrases, long? lastMessage = null)
    {
        return await SearchMessagesInternal(channelId, phrases);
    }
    
    public async Task<ChannelSearchMessages?> SearchMessages(string channelName, ICollection<string> phrases, long? lastMessage = null)
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
        
        return await SearchMessagesInternal(channel.Id, phrases);
    }

    public async Task<ChannelSearchMessages?> SearchMessagesInternal(long channelId, ICollection<string> phrases, long? lastMessage = null)
    {
        var phraseSearchMessages = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.ICollection<SearchMessage>>();
        var maxOffset = long.MinValue;
        foreach (var phrase in phrases)
        {
            var foundMessages = await _tdClient.SearchChatMessagesAsync(channelId, phrase, limit: 100, fromMessageId: lastMessage ?? 0L);
            if(foundMessages.Messages.Length == 0)
            {
                continue;
            }

            maxOffset = Math.Max(maxOffset, foundMessages.NextFromMessageId);

            var currentMessages = new List<SearchMessage>();
            phraseSearchMessages[phrase] = currentMessages;
            foreach (var foundMessage in foundMessages.Messages)
            {
                var messageLink = await _tdClient.GetMessageLinkAsync(channelId, foundMessage.Id);
                var messageDate = DateTimeOffset.FromUnixTimeSeconds(foundMessage.Date);
                var messageToAdd = new SearchMessage(foundMessage.Id, messageLink.Link, messageDate);
                
                currentMessages.Add(messageToAdd);
            }
        }
        
        return new ChannelSearchMessages(channelId, maxOffset, phraseSearchMessages);
    }
}
