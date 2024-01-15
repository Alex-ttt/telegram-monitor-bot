using Telegram.Bot.Types;
using TelegramMonitorBot.TelegramBotClient.Application.ChatBot.Common;

namespace TelegramMonitorBot.TelegramBotClient.Application.ChatBot.Queries.UnsubscribeFromChannel;

public record AskUnsubscribeFromChannelRequest(CallbackQuery CallbackQuery, long ChannelId) : CallbackQueryRequest(CallbackQuery);
