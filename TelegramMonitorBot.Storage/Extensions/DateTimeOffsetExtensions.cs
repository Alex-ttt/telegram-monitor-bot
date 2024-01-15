namespace TelegramMonitorBot.Storage.Extensions;

internal static class DateTimeOffsetExtensions
{
    internal static string ToISO_8601(this DateTimeOffset dateTimeOffset) => dateTimeOffset.ToString("O");
}
