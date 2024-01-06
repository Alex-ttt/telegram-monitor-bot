using MediatR;
using Telegram.Bot;
using TelegramMonitorBot.Storage.Repositories.Abstractions;
using TelegramMonitorBot.TelegramBotClient.ChatContext;

namespace TelegramMonitorBot.TelegramBotClient.Application.Commands.PrepareChannelForPhrasesAdding;

public class PrepareChannelForPhrasesAddingRequestHandler : IRequestHandler<PrepareChannelForPhrasesAddingRequest>
{
    private readonly ITelegramBotClient _botClient;
    private readonly ITelegramRepository _telegramRepository;
    private readonly ChatContextManager _chatContextManager;

    public PrepareChannelForPhrasesAddingRequestHandler(ITelegramBotClient botClient, ITelegramRepository telegramRepository, ChatContextManager chatContextManager)
    {
        _botClient = botClient;
        _telegramRepository = telegramRepository;
        _chatContextManager = chatContextManager;
    }

    public async Task Handle(PrepareChannelForPhrasesAddingRequest request, CancellationToken cancellationToken)
    {
        var chatId = request.CallbackQuery.Message!.Chat.Id;
        var channel = await _telegramRepository.GetChannel(request.ChannelId, cancellationToken);
        if (channel is null)
        {
            await _botClient.SendTextMessageAsync(
                chatId,
                "Канал не найден",
                cancellationToken: cancellationToken);
            
            return;
        }

        await _botClient.SendTextMessageAsync(
            chatId,
            $"Введите фразы для поиска по каналу @{channel.Name}. Можно написать несколько фраз - каждую с новой строки", 
            cancellationToken: cancellationToken);

        _chatContextManager.OnPhrasesAdding(chatId, channel.ChannelId);

        // TODO handle context states
    }
}
