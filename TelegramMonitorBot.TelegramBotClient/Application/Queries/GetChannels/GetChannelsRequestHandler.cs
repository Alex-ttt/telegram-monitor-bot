using MediatR;
using Telegram.Bot;
using TelegramMonitorBot.Domain.Models;
using TelegramMonitorBot.Storage.Repositories.Abstractions;
using TelegramMonitorBot.Storage.Repositories.Abstractions.Models;
using TelegramMonitorBot.TelegramBotClient.Application.Services;
using TelegramMonitorBot.TelegramBotClient.ChatContext;
using TelegramMonitorBot.TelegramBotClient.Extensions;
using TelegramMonitorBot.TelegramBotClient.Navigation;

namespace TelegramMonitorBot.TelegramBotClient.Application.Queries.GetChannels;

public class GetChannelsRequestHandler : IRequestHandler<GetChannelsRequest>
{
    private readonly ITelegramBotClient _botClient;
    private readonly ITelegramRepository _telegramRepository;
    private readonly BotNavigationManager _botNavigationManager;
    private readonly ChatContextManager _chatContextManager;

    public GetChannelsRequestHandler(
        ITelegramBotClient botClient, 
        ITelegramRepository telegramRepository, 
        BotNavigationManager botNavigationManager,
        ChatContextManager chatContextManager)
    {
        _botClient = botClient;
        _telegramRepository = telegramRepository;
        _botNavigationManager = botNavigationManager;
        _chatContextManager = chatContextManager;
    }

    public async Task Handle(GetChannelsRequest request, CancellationToken cancellationToken)
    {
        if (request.CallbackQuery.Message is not { } message)
        {
            throw new ArgumentNullException(nameof(request.CallbackQuery.Message), "Message on callback query can't be null");
        }
        
        var channels = await GetChannels(request, cancellationToken);

        var myChannelsRequest = _botNavigationManager.GetMyChannelsMessageRequest(message, channels);
        await _botClient.SendTextMessageRequestAsync(myChannelsRequest, cancellationToken);
        _chatContextManager.OnMainMenu(request.CallbackQuery.Message!.Chat.Id);
    }

    private async Task<PageResult<Channel>> GetChannels(GetChannelsRequest request, CancellationToken cancellationToken)
    {
        var channelsPager = ChannelService.GetDefaultChannelsListPager(request.Page);
        var channels = 
            await _telegramRepository.GetChannels(request.CallbackQuery.Message!.Chat.Id, channelsPager, cancellationToken);

        return channels;
    }
}
