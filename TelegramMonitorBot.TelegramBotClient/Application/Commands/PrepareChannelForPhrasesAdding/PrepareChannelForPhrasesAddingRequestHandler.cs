using MediatR;
using Telegram.Bot;
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

    public PrepareChannelForPhrasesAddingRequestHandler(
        ITelegramBotClient botClient, 
        IChannelUserRepository channelUserRepository, 
        ChatContextManager chatContextManager)
    {
        _botClient = botClient;
        _channelUserRepository = channelUserRepository;
        _chatContextManager = chatContextManager;
    }

    public async Task Handle(PrepareChannelForPhrasesAddingRequest request, CancellationToken cancellationToken)
    {
        var chatId = request.CallbackQuery.Message!.Chat.Id;
        var channel = await _channelUserRepository.GetChannel(request.ChannelId, cancellationToken);
        if (channel is null)
        {
            var channelNotFoundMessage = BotMessageBuilder.ChannelNotFound(chatId);
            await _botClient.SendTextMessageRequestAsync(channelNotFoundMessage, cancellationToken);

            return;
        }

        _chatContextManager.OnPhrasesAdding(chatId, channel.ChannelId);
        
        var message = BotMessageBuilder.GetPrepareChannelsForPhraseAddingMessage(chatId, channel);
        await _botClient.SendTextMessageRequestAsync(message, cancellationToken);

    }
}
