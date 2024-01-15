using Telegram.Bot.Types;
using TelegramMonitorBot.TelegramBotClient.Application.ChatBot.Common;

namespace TelegramMonitorBot.TelegramBotClient.Application.ChatBot.Queries.GetChannelPhrases;

public record GetChannelPhrasesRequest(CallbackQuery CallbackQuery, long ChannelId, int Page = 1) : CallbackQueryRequest(CallbackQuery);
