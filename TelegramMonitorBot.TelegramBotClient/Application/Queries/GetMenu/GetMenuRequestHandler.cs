using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramMonitorBot.TelegramBotClient.ChatContext;
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
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("Мои каналы", Routes.MyChannels), },
            new[] { InlineKeyboardButton.WithCallbackData("Подписаться", Routes.Subscribe), },
        });
        
        await _botClient.SendTextMessageAsync(
            chatId: request.ChatId,
            text: "Управление каналами",
            replyMarkup: inlineKeyboard,
            cancellationToken: cancellationToken);
        
        _chatContextManager.OnMainMenu(request.ChatId);
    }
}