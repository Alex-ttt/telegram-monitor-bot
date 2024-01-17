using TdLib;
using TelegramMonitorBot.TelegramApiClient.Models;
using TelegramMonitorBot.TelegramApiClient.Services;

using PhraseMessages = (string Phrase, System.Collections.Generic.ICollection<TelegramMonitorBot.TelegramApiClient.Models.SearchMessage> Messages);

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

    private async Task<ChannelSearchMessages> SearchMessagesInternal(long channelId, IEnumerable<string> phrases, long lastMessage = 0)
    {
        var phraseSearchMessages = new Dictionary<string, ICollection<SearchMessage>>();
        var history = await GetLastMessage(channelId);

        if (lastMessage is 0 || history is null)
        {
            return new ChannelSearchMessages(channelId, history?.Id ?? 0, phraseSearchMessages);
        }

        await foreach (var phraseMessages in GetMessages(channelId, lastMessage, phrases))
        {
            phraseSearchMessages.Add(phraseMessages.Phrase, phraseMessages.Messages);
        }

        return new ChannelSearchMessages(channelId, history.Id, phraseSearchMessages);
    }

    private async IAsyncEnumerable<PhraseMessages> GetMessages(long channelId, long lastMessage, IEnumerable<string> phrases)
    {
        foreach (var phrase in phrases)
        {
            // Now it works only for getting updates among 100 latest found messages
            // If there are more all messages above the limit will be lost
            // Improve algorithm in case of need of full search
            // To do so: keep searching with LastMessageId until lastMessage value is reached
            var foundMessages = await _tdClient.SearchChatMessagesAsync(channelId, phrase, limit: 100);
            if (foundMessages.Messages is not {Length: > 0} messages)
            {
                continue;
            }

            var tasks = messages
                .Where(t => t.Id > lastMessage)
                .Select(ConvertMessage);
            
            var result = await Task.WhenAll(tasks);

            if (result.Length != 0)
            {
                yield return (phrase, result);
            }
        }
    }
    
    private async Task<SearchMessage> ConvertMessage(TdApi.Message message)
    {
        var messageLink = await _tdClient.GetMessageLinkAsync(message.ChatId, message.Id);
        var messageDate = DateTimeOffset.FromUnixTimeSeconds(message.Date);
        var converted = new SearchMessage(message.Id, messageLink.Link, messageDate);
            
        return converted;
    }

    private async Task<TdApi.Message?> GetLastMessage(long channelId)
    {
        var messages = await _tdClient.GetChatHistoryAsync(channelId, limit: 1);
        return messages.Messages_?.FirstOrDefault();
    }
}
