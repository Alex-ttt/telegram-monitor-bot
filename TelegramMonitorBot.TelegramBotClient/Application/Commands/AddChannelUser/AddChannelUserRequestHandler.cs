using MediatR;
using Telegram.Bot;
using TelegramMonitorBot.Domain.Models;
using TelegramMonitorBot.Storage.Repositories.Abstractions;
using TelegramMonitorBot.TelegramApiClient;
using TelegramMonitorBot.TelegramBotClient.ChatContext;

namespace TelegramMonitorBot.TelegramBotClient.Application.Commands.AddChannelUser;

public class AddChannelUserRequestHandler : IRequestHandler<AddChannelUserRequest>
{
    private readonly ITelegramBotClient _botClient;
    private readonly ITelegramRepository _telegramRepository;
    private readonly ITelegramApiClient _telegramApiClient;
    private readonly ChatContextManager _chatContextManager;

    public AddChannelUserRequestHandler(
        ITelegramBotClient botClient, 
        ITelegramRepository telegramRepository, 
        ITelegramApiClient telegramApiClient, 
        ChatContextManager chatContextManager)
    {
        _botClient = botClient;
        _telegramRepository = telegramRepository;
        _telegramApiClient = telegramApiClient;
        _chatContextManager = chatContextManager;
    }

    public async Task Handle(AddChannelUserRequest request, CancellationToken cancellationToken)
    {
        var channel = await _telegramApiClient.FindChannelByName(request.ChannelName);
        var chatId = request.UserId;
        if (channel is null)
        {
            await _botClient.SendTextMessageAsync(
                chatId,
                $"Не удалось найти канал {request.ChannelName}",
                cancellationToken: cancellationToken);
            
            return;
        }

        var alreadySubscribed = await _telegramRepository.CheckChannelWithUser(channel.Id, request.UserId, cancellationToken);
        if (alreadySubscribed)
        {
            await _botClient.SendTextMessageAsync(
                chatId,
                $"Подписка на данный канал уже существует",
                cancellationToken: cancellationToken);

            return;
        }

        var userToPut = new User(request.UserId, request.UserName ?? "<unknown>");
        var channelToPut = new Channel(channel.Id, channel.Name);
        await _telegramRepository.PutUserChannel(userToPut, channelToPut, cancellationToken);
        
        _chatContextManager.OnAddedChannel(chatId);

        await _botClient.SendTextMessageAsync(chatId, $"Канал @{channel.Name} добавлен", cancellationToken: cancellationToken);
    }
}