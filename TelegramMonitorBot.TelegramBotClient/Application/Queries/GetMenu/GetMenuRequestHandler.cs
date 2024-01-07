using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramMonitorBot.TelegramBotClient.Application.Queries.GetMenu;

public class GetMenuRequestHandler : IRequestHandler<GetMenuRequest>
{
    private readonly ITelegramBotClient _botClient;

    public GetMenuRequestHandler(ITelegramBotClient botClient)
    {
        _botClient = botClient;
    }
    
    public async Task Handle(GetMenuRequest request, CancellationToken cancellationToken)
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("Мои каналы", "/my_channels"), },
            new[] { InlineKeyboardButton.WithCallbackData("Подписаться", "/subscribe"), },
        });
        
        await _botClient.SendTextMessageAsync(
            chatId: request.ChatId,
            text: "Управление каналами",
            replyMarkup: inlineKeyboard,
            cancellationToken: cancellationToken);
    }
}