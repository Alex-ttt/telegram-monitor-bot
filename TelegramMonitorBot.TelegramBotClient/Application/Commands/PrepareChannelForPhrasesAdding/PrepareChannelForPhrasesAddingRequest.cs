using Telegram.Bot.Types;
using TelegramMonitorBot.TelegramBotClient.Application.Common;

namespace TelegramMonitorBot.TelegramBotClient.Application.Commands.PrepareChannelForPhrasesAdding;

public record PrepareChannelForPhrasesAddingRequest(CallbackQuery CallbackQuery, long ChannelId) : CallbackQueryRequest(CallbackQuery);
