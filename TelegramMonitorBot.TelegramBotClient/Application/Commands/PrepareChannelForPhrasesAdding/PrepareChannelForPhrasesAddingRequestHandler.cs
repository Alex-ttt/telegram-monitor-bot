using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramMonitorBot.Storage.Repositories.Abstractions;
using TelegramMonitorBot.TelegramBotClient.ChatContext;
using TelegramMonitorBot.TelegramBotClient.Extensions;
using TelegramMonitorBot.TelegramBotClient.Navigation;

namespace TelegramMonitorBot.TelegramBotClient.Application.Commands.PrepareChannelForPhrasesAdding;

public class PrepareChannelForPhrasesAddingRequestHandler : IRequestHandler<PrepareChannelForPhrasesAddingRequest>
{
    private readonly ITelegramBotClient _botClient;
    private readonly IChannelUserRepository _channelUserRepository;
    private readonly ChatContextManager _chatContextManager;
    private readonly BotNavigationManager _botNavigationManager;

    public PrepareChannelForPhrasesAddingRequestHandler(
        ITelegramBotClient botClient, 
        IChannelUserRepository channelUserRepository, 
        ChatContextManager chatContextManager, 
        BotNavigationManager botNavigationManager)
    {
        _botClient = botClient;
        _channelUserRepository = channelUserRepository;
        _chatContextManager = chatContextManager;
        _botNavigationManager = botNavigationManager;
    }

    public async Task Handle(PrepareChannelForPhrasesAddingRequest request, CancellationToken cancellationToken)
    {
        var chatId = request.CallbackQuery.Message!.Chat.Id;
        var channel = await _channelUserRepository.GetChannel(request.ChannelId, cancellationToken);
        if (channel is null)
        {
            var channelNotFoundMessage = _botNavigationManager.ChannelNotFound(chatId);
            await _botClient.SendTextMessageRequestAsync(channelNotFoundMessage, cancellationToken);

            return;
        }

        var keyboardMarkup = new InlineKeyboardMarkup(
            new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("Назад к моим каналам", Routes.MyChannels), },
            });
        
        await _botClient.SendTextMessageAsync(
            chatId,
            $"Введите фразы для поиска по каналу @{channel.Name}. Можно написать несколько фраз - каждую с новой строки", 
            replyMarkup: keyboardMarkup,
            cancellationToken: cancellationToken);

        _chatContextManager.OnPhrasesAdding(chatId, channel.ChannelId);

        // TODO handle context states
    }
}
