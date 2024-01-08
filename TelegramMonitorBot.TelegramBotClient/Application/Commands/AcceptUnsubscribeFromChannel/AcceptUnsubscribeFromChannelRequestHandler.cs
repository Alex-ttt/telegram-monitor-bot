using MediatR;
using Telegram.Bot;
using TelegramMonitorBot.Domain.Models;
using TelegramMonitorBot.Storage.Repositories.Abstractions;
using TelegramMonitorBot.Storage.Repositories.Abstractions.Models;
using TelegramMonitorBot.TelegramBotClient.Application.Services;
using TelegramMonitorBot.TelegramBotClient.Extensions;
using TelegramMonitorBot.TelegramBotClient.Navigation;

namespace TelegramMonitorBot.TelegramBotClient.Application.Commands.AcceptUnsubscribeFromChannel;

public class AcceptUnsubscribeFromChannelRequestHandler : IRequestHandler<AcceptUnsubscribeFromChannelRequest>
{
    private readonly ITelegramBotClient _botClient;
    private readonly IChannelUserRepository _channelUserRepository;
    private readonly BotNavigationManager _botNavigationManager;

    public AcceptUnsubscribeFromChannelRequestHandler(ITelegramBotClient botClient, IChannelUserRepository channelUserRepository, BotNavigationManager botNavigationManager)
    {
        _botClient = botClient;
        _channelUserRepository = channelUserRepository;
        _botNavigationManager = botNavigationManager;
    }

    public async Task Handle(AcceptUnsubscribeFromChannelRequest request, CancellationToken cancellationToken)
    {
        
        var message = request.CallbackQuery.Message!;
        await _channelUserRepository.RemoveChannelUser(request.ChannelId, message.Chat.Id, cancellationToken);
        
        await _botClient.AnswerCallbackQueryAsync(request.CallbackQuery.Id, $"Вы успешно отписались от канала", cancellationToken: cancellationToken);
        
        var myChannels = await GetChannels(message.Chat.Id, cancellationToken);
        var messageToAnswer = _botNavigationManager.GetMyChannelsMessageRequest(message.Chat.Id, myChannels);
        await _botClient.SendTextMessageRequestAsync(messageToAnswer, cancellationToken);
    }
    
    private async Task<PageResult<Channel>> GetChannels(long chatId, CancellationToken cancellationToken)
    {
        var channelsPager = ChannelService.GetDefaultChannelsListPager(1);
        var channels = await _channelUserRepository.GetChannels(chatId, channelsPager, cancellationToken);

        return channels;
    }
}
