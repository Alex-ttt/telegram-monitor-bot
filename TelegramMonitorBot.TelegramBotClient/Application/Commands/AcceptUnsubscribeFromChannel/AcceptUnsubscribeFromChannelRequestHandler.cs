using MediatR;
using Telegram.Bot;
using TelegramMonitorBot.Domain.Models;
using TelegramMonitorBot.Storage.Repositories.Abstractions;
using TelegramMonitorBot.Storage.Repositories.Abstractions.Models;
using TelegramMonitorBot.TelegramBotClient.Application.Services;
using TelegramMonitorBot.TelegramBotClient.ChatContext;
using TelegramMonitorBot.TelegramBotClient.Extensions;
using TelegramMonitorBot.TelegramBotClient.Navigation;

namespace TelegramMonitorBot.TelegramBotClient.Application.Commands.AcceptUnsubscribeFromChannel;

public class AcceptUnsubscribeFromChannelRequestHandler : IRequestHandler<AcceptUnsubscribeFromChannelRequest>
{
    private readonly ITelegramBotClient _botClient;
    private readonly IChannelUserRepository _channelUserRepository;
    private readonly BotNavigationManager _botNavigationManager;
    private readonly ChatContextManager _chatContextManager;

    public AcceptUnsubscribeFromChannelRequestHandler(
        ITelegramBotClient botClient, 
        IChannelUserRepository channelUserRepository, 
        BotNavigationManager botNavigationManager, 
        ChatContextManager chatContextManager)
    {
        _botClient = botClient;
        _channelUserRepository = channelUserRepository;
        _botNavigationManager = botNavigationManager;
        _chatContextManager = chatContextManager;
    }

    public async Task Handle(AcceptUnsubscribeFromChannelRequest request, CancellationToken cancellationToken)
    {
        var chatId = request.CallbackQuery.Message!.Chat.Id;
        await _channelUserRepository.RemoveChannelUser(request.ChannelId, chatId, cancellationToken);
        
        await _botClient.AnswerCallbackQueryAsync(request.CallbackQuery.Id, $"Вы успешно отписались от канала", cancellationToken: cancellationToken);
        
        var myChannels = await GetChannels(chatId, cancellationToken);
        var messageToAnswer = _botNavigationManager.GetMyChannelsMessageRequest(chatId, myChannels);
        _chatContextManager.OnChannelRemoved(chatId);
        await _botClient.SendTextMessageRequestAsync(messageToAnswer, cancellationToken);
    }
    
    private async Task<PageResult<Channel>> GetChannels(long chatId, CancellationToken cancellationToken)
    {
        var channelsPager = ChannelService.GetDefaultChannelsListPager(1);
        var channels = await _channelUserRepository.GetChannels(chatId, channelsPager, cancellationToken);

        return channels;
    }
}
