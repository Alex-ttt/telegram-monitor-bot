using MediatR;
using Telegram.Bot;
using TelegramMonitorBot.TelegramBotClient.ChatContext;
using TelegramMonitorBot.TelegramBotClient.Extensions;
using TelegramMonitorBot.TelegramBotClient.Navigation;

namespace TelegramMonitorBot.TelegramBotClient.Application.Queries.GetMenu;

public class GetMenuRequestHandler : IRequestHandler<GetMenuRequest>
{
    private readonly ITelegramBotClient _botClient;
    private readonly ChatContextManager _chatContextManager;

    public GetMenuRequestHandler(
        ITelegramBotClient botClient,
        ChatContextManager chatContextManager)
    {
        _botClient = botClient;
        _chatContextManager = chatContextManager;
    }
    
    public async Task Handle(GetMenuRequest request, CancellationToken cancellationToken)
    {
        _chatContextManager.OnMainMenu(request.ChatId);

        var message = BotMessageBuilder.GetMenuMessage(request.ChatId);
        await _botClient.SendTextMessageRequestAsync(message, cancellationToken);
        
    }
}