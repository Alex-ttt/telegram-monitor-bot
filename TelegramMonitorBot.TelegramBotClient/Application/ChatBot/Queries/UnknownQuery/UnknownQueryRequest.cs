using MediatR;

namespace TelegramMonitorBot.TelegramBotClient.Application.ChatBot.Queries.UnknownQuery;

public record UnknownQueryRequest(long ChatId) : IRequest;
