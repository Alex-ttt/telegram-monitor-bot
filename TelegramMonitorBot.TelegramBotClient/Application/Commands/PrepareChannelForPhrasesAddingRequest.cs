using Telegram.Bot.Types;
using TelegramMonitorBot.TelegramBotClient.Application.Queries.Common;

namespace TelegramMonitorBot.TelegramBotClient.Application.Commands;

public record PrepareChannelForPhrasesAddingRequest(CallbackQuery CallbackQuery, long ChannelId) : CallbackQueryRequest(CallbackQuery);
