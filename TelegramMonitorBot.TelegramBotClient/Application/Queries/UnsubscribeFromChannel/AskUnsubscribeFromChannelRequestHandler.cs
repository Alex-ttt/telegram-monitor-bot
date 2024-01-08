using MediatR;
using Telegram.Bot;
using TelegramMonitorBot.Storage.Repositories.Abstractions;
using TelegramMonitorBot.TelegramBotClient.Extensions;
using TelegramMonitorBot.TelegramBotClient.Navigation;

namespace TelegramMonitorBot.TelegramBotClient.Application.Queries.UnsubscribeFromChannel;

public class AskUnsubscribeFromChannelRequestHandler : IRequestHandler<AskUnsubscribeFromChannelRequest>
{
    private readonly ITelegramBotClient _botClient;
    private readonly IChannelUserRepository _channelUserRepository;
    private readonly BotNavigationManager _botNavigationManager;

    public AskUnsubscribeFromChannelRequestHandler(ITelegramBotClient botClient, IChannelUserRepository channelUserRepository, BotNavigationManager botNavigationManager)
    {
        _botClient = botClient;
        _channelUserRepository = channelUserRepository;
        _botNavigationManager = botNavigationManager;
    }

    public async Task Handle(AskUnsubscribeFromChannelRequest request, CancellationToken cancellationToken)
    {
        var chatId = request.CallbackQuery.Message!.Chat.Id;
        var channel = await _channelUserRepository.GetChannel(request.ChannelId, cancellationToken);
        if (channel is null)
        {
            var channelNotFoundMessage = _botNavigationManager.ChannelNotFound(chatId);
            await _botClient.SendTextMessageRequestAsync(channelNotFoundMessage, cancellationToken);

            return;
        }
        
        var askUnsubscribeMessage = _botNavigationManager.AskUnsubscribeFromChannel(chatId, request.ChannelId, channel.Name);
        await _botClient.SendTextMessageRequestAsync(askUnsubscribeMessage, cancellationToken);
    }
}
