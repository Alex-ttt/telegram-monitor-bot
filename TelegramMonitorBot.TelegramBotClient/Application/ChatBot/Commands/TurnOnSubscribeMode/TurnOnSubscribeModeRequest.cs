using Telegram.Bot.Types;
using TelegramMonitorBot.TelegramBotClient.Application.ChatBot.Common;

namespace TelegramMonitorBot.TelegramBotClient.Application.ChatBot.Commands.TurnOnSubscribeMode;

public record TurnOnSubscribeModeRequest(CallbackQuery CallbackQuery) : CallbackQueryRequest(CallbackQuery);
