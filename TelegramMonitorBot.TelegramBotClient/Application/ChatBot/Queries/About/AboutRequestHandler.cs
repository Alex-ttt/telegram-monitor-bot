using MediatR;
using Telegram.Bot;
using TelegramMonitorBot.TelegramBotClient.ChatContext;
using TelegramMonitorBot.TelegramBotClient.Extensions;
using TelegramMonitorBot.TelegramBotClient.Navigation;

namespace TelegramMonitorBot.TelegramBotClient.Application.ChatBot.Queries.About;

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
        var aboutMessage = BotMessageBuilder.GetAbout(request.ChatId);
        await _botClient.SendTextMessageRequestAsync(aboutMessage, cancellationToken);
    }
}