using MediatR;
using Telegram.Bot;
using TelegramMonitorBot.TelegramBotClient.Navigation;

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
            $"Не удалось распознать текущую команду. Используйте {Routes.Menu}, чтобы перейти в главное меню", 
            cancellationToken: cancellationToken);
    }
}