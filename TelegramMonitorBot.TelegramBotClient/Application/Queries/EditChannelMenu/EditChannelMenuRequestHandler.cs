using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramMonitorBot.Storage.Repositories.Abstractions;
using TelegramMonitorBot.TelegramBotClient.Application.Services;

namespace TelegramMonitorBot.TelegramBotClient.Application.Queries.EditChannelMenu;

public class EditChannelMenuRequestHandler : IRequestHandler<EditChannelMenuRequest>
{
    private readonly ITelegramBotClient _botClient;
    private readonly ITelegramRepository _telegramRepository;

    public EditChannelMenuRequestHandler(ITelegramBotClient botClient, ITelegramRepository telegramRepository)
    {
        _botClient = botClient;
        _telegramRepository = telegramRepository;
    }

    public async Task Handle(EditChannelMenuRequest request, CancellationToken cancellationToken)
    {
        var channel = await _telegramRepository.GetChannel(request.ChannelId, cancellationToken);
        var chatId = request.CallbackQuery.Message!.Chat.Id;
        var channelId = request.ChannelId;
        if (channel is null)
        {
            await _botClient.SendTextMessageAsync(
                chatId,
                "Канал не найден",
                cancellationToken: cancellationToken);
            return;
        }
        
        var buttons = new[]
        {
            new[] { new InlineKeyboardButton("Добавить фразы для поиска") { CallbackData = $"/add_phrases_to_{channelId}"}},
            new[] { new InlineKeyboardButton("Удалить фразы для поиска") { CallbackData = $"/remove_phrases_{channelId}"}},
            new[] { new InlineKeyboardButton("Отписаться") { CallbackData = $"/unsubscribe_from_{channelId}"}},
            new[] { new InlineKeyboardButton("Перейти в канал") { Url = ChannelService.ChannelLink(channel.Name) }},
            new[] { new InlineKeyboardButton("Назад") { CallbackData = "/my_channels" }},
        };
        
        var keyboard = new InlineKeyboardMarkup(buttons);
        
        await  _botClient.SendTextMessageAsync(
            chatId,
            $"Настройка канала @{channel.Name}",
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);
    }
}
