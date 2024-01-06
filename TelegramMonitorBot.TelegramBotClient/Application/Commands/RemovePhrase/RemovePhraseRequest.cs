using Telegram.Bot.Types;
using TelegramMonitorBot.TelegramBotClient.Application.Common;

namespace TelegramMonitorBot.TelegramBotClient.Application.Commands.RemovePhrase;

public record RemovePhraseRequest(CallbackQuery CallbackQuery, long ChannelId, string Phrase) : CallbackQueryRequest(CallbackQuery);
