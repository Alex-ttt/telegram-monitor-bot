using TelegramMonitorBot.Storage.Repositories.Abstractions.Models;

namespace TelegramMonitorBot.TelegramBotClient.Application.Services;

public static class ChannelService
{
    // Move to separate settings place
    private const int DefaultChannelsListPageSize = 8;
    private const int DefaultPhrasesListPageSize = 8;
    public static string ChannelLink(string channelName) => $"https://t.me/{channelName}";

    public static Pager GetDefaultChannelsListPager(int? page = null) => new(page ?? 1, DefaultChannelsListPageSize);
    public static Pager GetDefaultPhrasesListPager(int? page = null) => new(page ?? 1, DefaultPhrasesListPageSize);
}
