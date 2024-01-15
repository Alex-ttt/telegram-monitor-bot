using Telegram.Bot.Types;
using TelegramMonitorBot.TelegramBotClient.Application.ChatBot.Common;

namespace TelegramMonitorBot.TelegramBotClient.Application.ChatBot.Commands.PrepareChannelForPhrasesAdding;

public record PrepareChannelForPhrasesAddingRequest(CallbackQuery CallbackQuery, long ChannelId) : CallbackQueryRequest(CallbackQuery);
