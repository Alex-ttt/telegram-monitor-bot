using Telegram.Bot.Types;
using TelegramMonitorBot.TelegramBotClient.Application.Common;

namespace TelegramMonitorBot.TelegramBotClient.Application.Queries.GetChannels;

public record GetChannelsRequest(CallbackQuery CallbackQuery, int? Page) : CallbackQueryRequest(CallbackQuery);
