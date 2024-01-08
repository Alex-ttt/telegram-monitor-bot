using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramMonitorBot.Storage.Repositories.Abstractions;
using TelegramMonitorBot.TelegramBotClient.Application.Services;
using TelegramMonitorBot.TelegramBotClient.Extensions;
using TelegramMonitorBot.TelegramBotClient.Navigation;

namespace TelegramMonitorBot.TelegramBotClient.Application.Queries.EditChannelMenu;

public class EditChannelMenuRequestHandler : IRequestHandler<EditChannelMenuRequest>
{
    private readonly ITelegramBotClient _botClient;
    private readonly IChannelUserRepository _channelUserRepository;
    private readonly BotNavigationManager _botNavigationManager;

    public EditChannelMenuRequestHandler(
        ITelegramBotClient botClient,
        IChannelUserRepository channelUserRepository, 
        BotNavigationManager botNavigationManager)
    {
        _botClient = botClient;
        _channelUserRepository = channelUserRepository;
        _botNavigationManager = botNavigationManager;
    }

    public async Task Handle(EditChannelMenuRequest request, CancellationToken cancellationToken)
    {
        var channel = await _channelUserRepository.GetChannel(request.ChannelId, cancellationToken);
        var chatId = request.CallbackQuery.Message!.Chat.Id;
        var channelId = request.ChannelId;
        if (channel is null)
        {
            var channelNotFoundMessage = _botNavigationManager.ChannelNotFound(chatId);
            await _botClient.SendTextMessageRequestAsync(channelNotFoundMessage, cancellationToken);

            return;
        }
        
        var buttons = new[]
        {
            new[] { new InlineKeyboardButton("Добавить фразы для поиска") { CallbackData = Routes.AddPhrases(channelId)}},
            new[] { new InlineKeyboardButton("Список фраз для поиска") { CallbackData =Routes.ShowChannelPhrases(channelId)}},
            new[] { new InlineKeyboardButton("Отписаться") { CallbackData = Routes.UnsubscribeFrom(channelId)}},
            new[] { new InlineKeyboardButton("Перейти в канал") { Url = ChannelService.ChannelLink(channel.Name) }},
            new[] { new InlineKeyboardButton("Назад") { CallbackData = Routes.MyChannels }},
        };
        
        var keyboard = new InlineKeyboardMarkup(buttons);
        
        await  _botClient.SendTextMessageAsync(
            chatId,
            $"Настройка канала @{channel.Name}",
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);
    }
}
