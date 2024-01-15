using MediatR;
using Telegram.Bot;
using TelegramMonitorBot.TelegramBotClient.ChatContext;
using TelegramMonitorBot.TelegramBotClient.Extensions;
using TelegramMonitorBot.TelegramBotClient.Navigation;

namespace TelegramMonitorBot.TelegramBotClient.Application.ChatBot.Commands.TurnOnSubscribeMode;

public class TurnOnSubscribeModeRequestHandler : IRequestHandler<TurnOnSubscribeModeRequest>
{
    private readonly ITelegramBotClient _botClient;
    private readonly ChatContextManager _contextManager;

    public TurnOnSubscribeModeRequestHandler(
        ITelegramBotClient botClient, 
        ChatContextManager contextManager)
    {
        _botClient = botClient;
        _contextManager = contextManager;
    }

    public async Task Handle(TurnOnSubscribeModeRequest request, CancellationToken cancellationToken)
    {
        var chatId = request.CallbackQuery.Message!.Chat.Id;
        var message = BotMessageBuilder.GetInputChannelNameMessage(chatId);
        await _botClient.SendTextMessageRequestAsync(message, cancellationToken);
        
        _contextManager.OnAddingChannel(chatId);
    }
}