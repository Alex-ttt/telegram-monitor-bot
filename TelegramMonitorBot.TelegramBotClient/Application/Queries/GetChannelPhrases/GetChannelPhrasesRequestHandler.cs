using MediatR;
using Telegram.Bot;
using TelegramMonitorBot.Storage.Repositories.Abstractions;
using TelegramMonitorBot.TelegramBotClient.ChatContext;
using TelegramMonitorBot.TelegramBotClient.Extensions;
using TelegramMonitorBot.TelegramBotClient.Navigation;

namespace TelegramMonitorBot.TelegramBotClient.Application.Queries.GetChannelPhrases;

public class GetChannelPhrasesRequestHandler : IRequestHandler<GetChannelPhrasesRequest>
{
    private readonly ITelegramBotClient _botClient;
    private readonly IChannelUserRepository _channelUserRepository;
    private readonly ChatContextManager _contextManager;

    public GetChannelPhrasesRequestHandler(
        ITelegramBotClient botClient,
        IChannelUserRepository channelUserRepository, 
        ChatContextManager contextManager)
    {
        _botClient = botClient;
        _channelUserRepository = channelUserRepository;
        _contextManager = contextManager;
    }

    public async Task Handle(GetChannelPhrasesRequest request, CancellationToken cancellationToken)
    {
        var channelId = request.ChannelId;
        var chatId = request.CallbackQuery.Message!.Chat.Id;
        var phrases = await _channelUserRepository.GetChannelUserPhrases(channelId, chatId, cancellationToken);

        var channel = await _channelUserRepository.GetChannel(channelId, cancellationToken);
        if (channel is null)
        {
            var channelNotFoundMessage = BotMessageBuilder.ChannelNotFound(chatId);
            await _botClient.SendTextMessageRequestAsync(channelNotFoundMessage, cancellationToken);

            return;
        }

        _contextManager.OnGetChannelPhrases(chatId);
        var channelPhrasesRequest = BotMessageBuilder.GetChannelPhrasesRequest(chatId, channel, phrases, request.Page);
        await _botClient.SendTextMessageRequestAsync(channelPhrasesRequest, cancellationToken);

    }
}
