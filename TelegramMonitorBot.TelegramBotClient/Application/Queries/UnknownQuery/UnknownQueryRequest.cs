using MediatR;

namespace TelegramMonitorBot.TelegramBotClient.Application.Queries.UnknownQuery;

public record UnknownQueryRequest(long ChatId) : IRequest;
