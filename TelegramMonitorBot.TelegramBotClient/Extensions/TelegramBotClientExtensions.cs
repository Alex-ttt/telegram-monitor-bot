using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramMonitorBot.TelegramBotClient.Navigation.Models;

namespace TelegramMonitorBot.TelegramBotClient.Extensions;

internal static class TelegramBotClientExtensions
{
    internal static Task<Message> SendTextMessageRequestAsync(
        this ITelegramBotClient botClient,
        MessageRequest messageRequest,
        CancellationToken cancellationToken = default)
    {
        return botClient.SendTextMessageAsync(
            messageRequest.ChatId,
            messageRequest.Text,
            replyMarkup: messageRequest.ReplyMarkup,
            cancellationToken: cancellationToken);
    }
}