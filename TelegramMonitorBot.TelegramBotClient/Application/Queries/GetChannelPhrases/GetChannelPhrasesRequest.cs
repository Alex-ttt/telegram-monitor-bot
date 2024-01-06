using Telegram.Bot.Types;
using TelegramMonitorBot.TelegramBotClient.Application.Queries.Common;

namespace TelegramMonitorBot.TelegramBotClient.Application.Queries.GetChannelPhrases;

public record GetChannelPhrasesRequest(CallbackQuery CallbackQuery, long ChannelId, int Page = 1) : CallbackQueryRequest(CallbackQuery);
