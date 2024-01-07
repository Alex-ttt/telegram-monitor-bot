using MediatR;
using Telegram.Bot;

namespace TelegramMonitorBot.TelegramBotClient.Application.Queries.About;

public class AboutRequestHandler : IRequestHandler<AboutRequest>
{
    private readonly ITelegramBotClient _botClient;

    public AboutRequestHandler(ITelegramBotClient botClient)
    {
        _botClient = botClient;
    }

    public Task Handle(AboutRequest request, CancellationToken cancellationToken)
    {
        return _botClient.SendTextMessageAsync(
            request.ChatId,
            "Этот бот создан для того, чтобы помочь отслеживать появление ключевых слов в публичных каналах телеграма",
            cancellationToken: cancellationToken);
    }
}