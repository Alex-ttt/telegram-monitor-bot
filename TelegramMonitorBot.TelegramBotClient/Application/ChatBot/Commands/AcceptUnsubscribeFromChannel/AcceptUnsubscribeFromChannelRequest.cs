using Telegram.Bot.Types;
using TelegramMonitorBot.TelegramBotClient.Application.ChatBot.Common;

namespace TelegramMonitorBot.TelegramBotClient.Application.ChatBot.Commands.AcceptUnsubscribeFromChannel;

public record AcceptUnsubscribeFromChannelRequest(CallbackQuery CallbackQuery, long ChannelId) : CallbackQueryRequest(CallbackQuery);
