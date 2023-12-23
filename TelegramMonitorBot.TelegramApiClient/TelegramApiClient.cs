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
            Title = searchPublicChatResult.Title,
            LastMessageId = searchPublicChatResult.LastMessage.Id,
        };

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