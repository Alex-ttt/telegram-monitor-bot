using MediatR;

namespace TelegramMonitorBot.TelegramBotClient.Application.Common;

public record IgnoreQueryRequest : IRequest
{
    public static readonly IgnoreQueryRequest Instance = new();
}

public class IgnoreQueryRequestHandler : IRequestHandler<IgnoreQueryRequest>
{
    public Task Handle(IgnoreQueryRequest request, CancellationToken cancellationToken) => Task.CompletedTask;
}
