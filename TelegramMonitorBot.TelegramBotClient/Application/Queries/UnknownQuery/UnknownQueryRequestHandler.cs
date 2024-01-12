using MediatR;
using Telegram.Bot;
using TelegramMonitorBot.TelegramBotClient.Extensions;
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
        var message = BotMessageBuilder.GetUnrecognizedRequestMessage(request.ChatId);
        await _botClient.SendTextMessageRequestAsync(message, cancellationToken);
    }
}