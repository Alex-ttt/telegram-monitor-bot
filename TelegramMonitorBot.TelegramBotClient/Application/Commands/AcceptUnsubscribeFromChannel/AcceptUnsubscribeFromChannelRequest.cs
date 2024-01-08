using Telegram.Bot.Types;
using TelegramMonitorBot.TelegramBotClient.Application.Common;

namespace TelegramMonitorBot.TelegramBotClient.Application.Commands.AcceptUnsubscribeFromChannel;

public record AcceptUnsubscribeFromChannelRequest(CallbackQuery CallbackQuery, long ChannelId) : CallbackQueryRequest(CallbackQuery);
