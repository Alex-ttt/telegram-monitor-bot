namespace TelegramMonitorBot.TelegramBotClient.Application.Services;

public static class ChannelService
{
    public static string ChannelLink(string channelName) => $@"https://t.me/{channelName}";
}