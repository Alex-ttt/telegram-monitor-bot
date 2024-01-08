using Telegram.Bot.Types;
using TelegramMonitorBot.TelegramBotClient.Application.Common;

namespace TelegramMonitorBot.TelegramBotClient.Application.Queries.UnsubscribeFromChannel;

public record AskUnsubscribeFromChannelRequest(CallbackQuery CallbackQuery, long ChannelId) : CallbackQueryRequest(CallbackQuery);
