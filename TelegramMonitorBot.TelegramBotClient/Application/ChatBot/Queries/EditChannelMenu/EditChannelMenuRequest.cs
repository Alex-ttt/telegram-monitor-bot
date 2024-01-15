using Telegram.Bot.Types;
using TelegramMonitorBot.TelegramBotClient.Application.ChatBot.Common;

namespace TelegramMonitorBot.TelegramBotClient.Application.ChatBot.Queries.EditChannelMenu;

public record EditChannelMenuRequest(CallbackQuery CallbackQuery, long ChannelId) : CallbackQueryRequest(CallbackQuery);
