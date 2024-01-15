using Telegram.Bot.Types;
using TelegramMonitorBot.TelegramBotClient.Application.ChatBot.Common;

namespace TelegramMonitorBot.TelegramBotClient.Application.ChatBot.Commands.RemovePhrase;

public record RemovePhraseRequest(CallbackQuery CallbackQuery, long ChannelId, string Phrase) : CallbackQueryRequest(CallbackQuery);
