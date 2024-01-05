using MediatR;
using Telegram.Bot.Types;

namespace TelegramMonitorBot.TelegramBotClient.Application.Queries.Common;

public record CallbackQueryRequest<TResponse>(CallbackQuery CallbackQuery) : IRequest<TResponse>;

public record CallbackQueryRequest(CallbackQuery CallbackQuery) : IRequest;