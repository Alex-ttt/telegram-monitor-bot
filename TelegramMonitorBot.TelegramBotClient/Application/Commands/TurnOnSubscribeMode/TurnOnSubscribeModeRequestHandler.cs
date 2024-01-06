using MediatR;
using Telegram.Bot;
using TelegramMonitorBot.TelegramBotClient.ChatContext;

namespace TelegramMonitorBot.TelegramBotClient.Application.Commands.TurnOnSubscribeMode;

public class TurnOnSubscribeModeRequestHandler : IRequestHandler<TurnOnSubscribeModeRequest>
{
    private readonly ITelegramBotClient _botClient;
    private readonly ChatContextManager _contextManager;

    public TurnOnSubscribeModeRequestHandler(ITelegramBotClient botClient, ChatContextManager contextManager)
    {
        _botClient = botClient;
        _contextManager = contextManager;
    }

    public async Task Handle(TurnOnSubscribeModeRequest request, CancellationToken cancellationToken)
    {
        var chatId = request.CallbackQuery.Message!.Chat.Id;
        await _botClient.SendTextMessageAsync(
            chatId,
            "Введите короткие имя (начинается с @) или ссылку на канал, чтобы подписаться на него", 
            cancellationToken: cancellationToken);
        
        _contextManager.OnAddingChannel(chatId);
    }
}