using MediatR;
using Telegram.Bot;

namespace TelegramMonitorBot.TelegramBotClient.Application.Queries.UnknownQuery;

public class UnknownQueryRequestHandler : IRequestHandler<UnknownQueryRequest>
{
    private readonly ITelegramBotClient _botClient;

    public UnknownQueryRequestHandler(ITelegramBotClient botClient)
    {
        _botClient = botClient;
    }

    public async Task Handle(UnknownQueryRequest request, CancellationToken cancellationToken)
    {
        await _botClient.SendTextMessageAsync(
            request.ChatId, 
            "Не удалось распознать текущую команду. Используйте /menu, чтобы перейти в главное меню", 
            cancellationToken: cancellationToken);
    }
}