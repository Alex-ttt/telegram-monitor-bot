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

    public async Task<Channel?> GetChannel(long channelId)
    {
        var accessibleChannel = await GetChannelById(channelId);
        if (accessibleChannel is not null)
        {
            return accessibleChannel;
        }

        var joinChannelResult = await JoinChannel(channelId);
        if (joinChannelResult is false)
        {
            return null;
        }
        
        var newChannel = await GetChannelById(channelId);

        return newChannel;
    }

    private async Task<bool> JoinChannel(long channelId)
    {
        // _tdClient.Search
        
        var result = await _tdClient.JoinChatAsync(channelId);
        return result is not null;
    }
    
    
    private async Task<Channel?> GetChannelById(long channelId)
    {
        try
        {
            var channel = await _tdClient.GetChatAsync(channelId);
            var channelUserName = await GetChannelUserName(channel);

            return new Channel
            {
                Id = channel.Id,
                Name = channelUserName,
            };
        }
        catch (TdException ex) when (ex.Message == "Chat not found")
        {
            return null;
        }
    }

    private async Task<string> GetChannelUserName(TdApi.Chat chat)
    {
        if (chat.Type is TdApi.ChatType.ChatTypeSupergroup { } supergroup)
        {
            var superGroup = await _tdClient.GetSupergroupAsync(supergroup.SupergroupId);
            return superGroup.Usernames.ActiveUsernames.FirstOrDefault() ?? "<unknown>";
        }

        return string.Empty;
    }
}