using System.Text.RegularExpressions;

namespace TelegramMonitorBot.TelegramApiClient.Services;

internal static class ChannelService
{
    private const string RegexChannelNameGroup = "channelName";
    private static readonly Regex ChannelNameRegex = new(@$"(^((https):\/\/t\.me\/)|(^\@))?(?'{RegexChannelNameGroup}'[a-zA-Z_]+\w*$)", RegexOptions.Compiled);
    
    internal static string ParseChannel(string channel)
    {
        var match = ChannelNameRegex.Match(channel.ToLower().Trim());
        if (match.Success && match.Groups[RegexChannelNameGroup] is {Success: true} group)
        {
            return group.Value;
        }
            
        throw new Exception("Incorrect channel name");
    }
}
