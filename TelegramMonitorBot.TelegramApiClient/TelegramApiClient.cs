using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using TdLib;
using TelegramMonitorBot.TelegramApiClient.Models;

namespace TelegramMonitorBot.TelegramApiClient;

// TODO Add caching

internal class TelegramApiClient : ITelegramApiClient
{
    private readonly TdClient _tdClient;
    private readonly ILogger _logger;

    // TODO Move to separate service
    private const string RegexChannelNameGroup = "channelName";
    private static readonly Regex _channelNameRegex = new(
        @$"(^((https):\/\/t\.me\/)|(^\@))?(?'{RegexChannelNameGroup}'[a-zA-Z_]+\w*$)", 
        RegexOptions.Compiled);
    
    internal TelegramApiClient(TdClient tdClient, ILogger<TelegramApiClient> logger) =>
        (_tdClient, _logger) = (tdClient, logger);

    public async Task DoStuff()
    {
        var user1 = await _tdClient.SearchUserByPhoneNumberAsync("****");
        var user2 = await _tdClient.GetUserAsync(77777);
        var chat = await _tdClient.SearchPublicChatAsync("****");

        
        _logger.LogInformation("It works!");
    }

    public async Task<Channel?> FindChannelByName(string channel)
    {
        var channelName = ParseChannel(channel);

        TdApi.Chat? searchPublicChatResult = null;
        try
        {
            searchPublicChatResult = await _tdClient.SearchPublicChatAsync(channelName);
        }
        catch (TdException ex) when (ex.Message == "USERNAME_NOT_OCCUPIED")
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

    private static string ParseChannel(string channel)
    {
        var match = _channelNameRegex.Match(channel.Trim());
        if (
            match.Success is false 
            || match.Groups.TryGetValue(RegexChannelNameGroup, out var group) is false)
        {
            throw new Exception("Incorrect channel name");
        }

        return group.Value;
    }
}