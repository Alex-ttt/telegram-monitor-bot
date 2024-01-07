using MediatR;

namespace TelegramMonitorBot.TelegramBotClient.Application.Queries.About;

public record AboutRequest(long ChatId) : IRequest;
