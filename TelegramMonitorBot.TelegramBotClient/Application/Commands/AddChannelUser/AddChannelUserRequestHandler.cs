using MediatR;
using Telegram.Bot;
using TelegramMonitorBot.Domain.Models;
using TelegramMonitorBot.Storage.Repositories.Abstractions;
using TelegramMonitorBot.TelegramApiClient;
using TelegramMonitorBot.TelegramBotClient.ChatContext;
using TelegramMonitorBot.TelegramBotClient.Extensions;
using TelegramMonitorBot.TelegramBotClient.Navigation;

namespace TelegramMonitorBot.TelegramBotClient.Application.Commands.AddChannelUser;

public class AddChannelUserRequestHandler : IRequestHandler<AddChannelUserRequest>
{
    private readonly ITelegramBotClient _botClient;
    private readonly IChannelUserRepository _channelUserRepository;
    private readonly ITelegramApiClient _telegramApiClient;
    private readonly ChatContextManager _chatContextManager;
    private readonly BotNavigationManager _botNavigationManager;

    public AddChannelUserRequestHandler(
        ITelegramBotClient botClient, 
        IChannelUserRepository channelUserRepository, 
        ITelegramApiClient telegramApiClient, 
        ChatContextManager chatContextManager,
        BotNavigationManager botNavigationManager)
    {
        _botClient = botClient;
        _channelUserRepository = channelUserRepository;
        _telegramApiClient = telegramApiClient;
        _chatContextManager = chatContextManager;
        _botNavigationManager = botNavigationManager;
    }

    public async Task Handle(AddChannelUserRequest request, CancellationToken cancellationToken)
    {
        var chatId = request.UserId;
        var channel = await _telegramApiClient.FindChannelByName(request.ChannelName);
        if (channel is null)
        {
            await SendChannelNotFound(chatId, cancellationToken);
            return;
        }

        var alreadySubscribed = await _channelUserRepository.CheckChannelWithUser(channel.Id, request.UserId, cancellationToken);
        if (alreadySubscribed)
        {
            await SendAlreadySubscribed(chatId, channel.Name, cancellationToken);
            return;
        }

        await SubscribeAndSendMessage(request, channel, cancellationToken);
    }

    private async Task SubscribeAndSendMessage(AddChannelUserRequest request, TelegramApiClient.Models.Channel channel,CancellationToken cancellationToken)
    {
        var userToPut = new User(request.UserId, request.UserName ?? "<unknown>");
        var channelToPut = new Channel(channel.Id, channel.Name);
        await _channelUserRepository.PutUserChannel(userToPut, channelToPut, cancellationToken);

        _chatContextManager.OnAddedChannel(request.UserId);

        var channelAddedMessage = _botNavigationManager.ChannelAdded(request.UserId, channel.Name);
        await _botClient.SendTextMessageRequestAsync(channelAddedMessage, cancellationToken);
    }

    private async Task SendChannelNotFound(long chatId, CancellationToken cancellationToken)
    {
        var channelNotFoundMessage = _botNavigationManager.ChannelNotFound(chatId);
        await _botClient.SendTextMessageRequestAsync(channelNotFoundMessage, cancellationToken);
    }

    private async Task SendAlreadySubscribed(long chatId, string channelName, CancellationToken cancellationToken)
    {
        var alreadySubscribedMessage = _botNavigationManager.AlreadySubscribed(chatId, channelName);
        await _botClient.SendTextMessageRequestAsync(alreadySubscribedMessage, cancellationToken);
    }
}