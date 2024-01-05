using MediatR;
using Telegram.Bot.Types;
using TelegramMonitorBot.TelegramBotClient.Application.Queries.Common;

namespace TelegramMonitorBot.TelegramBotClient.Application.Queries.MyChannels;

public record GetMyChannelsRequest(CallbackQuery CallbackQuery) : CallbackQueryRequest<GetMyChannelsResponse>(CallbackQuery);
