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

    public async Task<ChannelSearchMessages> SearchMessages(long channelId, ICollection<string> phrases,
        long lastMessage = 0)
    {
        return await SearchMessagesInternal(channelId, phrases, lastMessage);
    }

    public async Task<ChannelSearchMessages?> SearchMessages(string channelName, ICollection<string> phrases, long lastMessage = 0)
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

        return await SearchMessagesInternal(channel.Id, phrases, lastMessage);
    }

    internal async Task<ChannelSearchMessages> SearchMessagesInternal(long channelId, IEnumerable<string> phrases, long lastMessage = 0)
    {
        var phraseSearchMessages = new Dictionary<string, ICollection<SearchMessage>>();
        if (lastMessage is 0)
        {
            var history = await GetLastMessage(channelId);
            return new ChannelSearchMessages(channelId, history?.Id ?? 0, phraseSearchMessages);
        }
        
        var maxMessageId = long.MinValue;

        foreach (var phrase in phrases)
        {
            var foundMessages = await _tdClient.SearchChatMessagesAsync(channelId, phrase, limit: 100);

            if (foundMessages.Messages is not {Length: > 0} messages)
            {
                continue;
            }

            maxMessageId = Math.Max(maxMessageId, messages.First().Id);
            foreach (var foundMessage in messages)
            {
                if (lastMessage >= foundMessage.Id)
                {
                    continue;
                }
                
                if (phraseSearchMessages.TryGetValue(phrase, out var currentMessages) is false)
                {
                    currentMessages = new List<SearchMessage>();
                    phraseSearchMessages[phrase] = currentMessages;
                }
                
                var messageLink = await _tdClient.GetMessageLinkAsync(channelId, foundMessage.Id);
                var messageDate = DateTimeOffset.FromUnixTimeSeconds(foundMessage.Date);
                var messageToAdd = new SearchMessage(foundMessage.Id, messageLink.Link, messageDate);

                currentMessages.Add(messageToAdd);
            }
        }

        if (phraseSearchMessages.Count == 0)
        {
            // Set the last message in chat as last found message for the next search
            var message = await GetLastMessage(channelId);
            maxMessageId = message?.Id ?? 0L;
        }

        return new ChannelSearchMessages(channelId, maxMessageId, phraseSearchMessages);
    }

    private async Task<TdApi.Message?> GetLastMessage(long channelId)
    {
        var messages = await _tdClient.GetChatHistoryAsync(channelId, limit: 1);
        return messages.Messages_?.FirstOrDefault();
    }
}
