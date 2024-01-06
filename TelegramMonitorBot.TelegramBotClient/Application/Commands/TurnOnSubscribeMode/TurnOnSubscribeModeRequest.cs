using Telegram.Bot.Types;
using TelegramMonitorBot.TelegramBotClient.Application.Common;

namespace TelegramMonitorBot.TelegramBotClient.Application.Commands.TurnOnSubscribeMode;

public record TurnOnSubscribeModeRequest(CallbackQuery CallbackQuery) : CallbackQueryRequest(CallbackQuery);
