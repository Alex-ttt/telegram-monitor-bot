using MediatR;
using Telegram.Bot;
using TelegramMonitorBot.TelegramBotClient.ChatContext;

namespace TelegramMonitorBot.TelegramBotClient.Application.Queries.About;

public class AboutRequestHandler : IRequestHandler<AboutRequest>
{
    private readonly ITelegramBotClient _botClient;
    private readonly ChatContextManager _contextManager;

    public AboutRequestHandler(
        ITelegramBotClient botClient, 
        ChatContextManager contextManager)
    {
        _botClient = botClient;
        _contextManager = contextManager;
    }

    public async Task Handle(AboutRequest request, CancellationToken cancellationToken)
    {
        _contextManager.OnAbout(request.ChatId);
        await _botClient.SendTextMessageAsync(
            request.ChatId,
            "Этот бот создан для того, чтобы помочь отслеживать появление ключевых слов в публичных каналах телеграма",
            cancellationToken: cancellationToken);
    }
}