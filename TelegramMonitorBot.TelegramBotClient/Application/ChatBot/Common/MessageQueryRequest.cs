using MediatR;
using Telegram.Bot.Types;

namespace TelegramMonitorBot.TelegramBotClient.Application.ChatBot.Common;

public record MessageQueryRequest<TResponse>(Message Message) : IRequest<TResponse>;

public record MessageQueryRequest(Message Message) : IRequest;
