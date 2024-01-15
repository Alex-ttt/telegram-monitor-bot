using MediatR;
using Telegram.Bot;
using TelegramMonitorBot.Domain.Models;
using TelegramMonitorBot.Storage.Repositories.Abstractions;
using TelegramMonitorBot.Storage.Repositories.Abstractions.Models;
using TelegramMonitorBot.TelegramBotClient.Application.Services;
using TelegramMonitorBot.TelegramBotClient.ChatContext;
using TelegramMonitorBot.TelegramBotClient.Extensions;
using TelegramMonitorBot.TelegramBotClient.Navigation;

namespace TelegramMonitorBot.TelegramBotClient.Application.ChatBot.Queries.GetChannels;

public class GetChannelsRequestHandler : IRequestHandler<GetChannelsRequest>
{
    private readonly ITelegramBotClient _botClient;
    private readonly IChannelUserRepository _channelUserRepository;
    private readonly ChatContextManager _chatContextManager;

    public GetChannelsRequestHandler(
        ITelegramBotClient botClient, 
        IChannelUserRepository channelUserRepository, 
        ChatContextManager chatContextManager)
    {
        _botClient = botClient;
        _channelUserRepository = channelUserRepository;
        _chatContextManager = chatContextManager;
    }

    public async Task Handle(GetChannelsRequest request, CancellationToken cancellationToken)
    {
        var channels = await GetChannels(request, cancellationToken);

        var myChannelsRequest = BotMessageBuilder.GetMyChannelsMessageRequest(request.ChatId, channels);
        await _botClient.SendTextMessageRequestAsync(myChannelsRequest, cancellationToken);
        _chatContextManager.OnChannelsList(request.ChatId);
    }

    private async Task<PageResult<Channel>> GetChannels(GetChannelsRequest request, CancellationToken cancellationToken)
    {
        var channelsPager = ChannelService.GetDefaultChannelsListPager(request.Page);
        var channels = 
            await _channelUserRepository.GetChannels(request.ChatId, channelsPager, cancellationToken);

        return channels;
    }
}
