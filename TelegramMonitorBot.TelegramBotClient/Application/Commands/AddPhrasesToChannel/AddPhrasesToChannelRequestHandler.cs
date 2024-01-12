using MediatR;
using Telegram.Bot;
using TelegramMonitorBot.Domain.Models;
using TelegramMonitorBot.Storage.Repositories.Abstractions;
using TelegramMonitorBot.TelegramBotClient.ChatContext;
using TelegramMonitorBot.TelegramBotClient.Extensions;
using TelegramMonitorBot.TelegramBotClient.Navigation;

namespace TelegramMonitorBot.TelegramBotClient.Application.Commands.AddPhrasesToChannel;

public class AddPhrasesToChannelRequestHandler : IRequestHandler<AddPhrasesToChannelRequest>
{
    private readonly ITelegramBotClient _botClient;
    private readonly IChannelUserRepository _channelUserRepository;
    private readonly ChatContextManager _chatContextManager;

    private const int PhraseMaxLength = 30;

    public AddPhrasesToChannelRequestHandler(
        ITelegramBotClient botClient, 
        IChannelUserRepository channelUserRepository, 
        ChatContextManager chatContextManager)
    {
        _botClient = botClient;
        _channelUserRepository = channelUserRepository;
        _chatContextManager = chatContextManager;
    }

    public async Task Handle(AddPhrasesToChannelRequest request, CancellationToken cancellationToken)
    {
        var channel = await _channelUserRepository.GetChannel(request.ChannelId, cancellationToken);
        if (channel is null)
        {
            var channelNotFoundMessage = BotMessageBuilder.ChannelNotFound(request.UserId);
            await _botClient.SendTextMessageRequestAsync(channelNotFoundMessage, cancellationToken);
            
            return;
        }
        
        var phrases = request.Phrases
            .Select(t => t.Trim())
            .Where(t => t.Length > 0)
            .Distinct()
            .ToList();
        
        if (phrases.Any(t => t.Length > PhraseMaxLength))
        {
            var tooLongPhrasesMessage = BotMessageBuilder.GetPhrasesTooLongMessage(request.UserId, PhraseMaxLength);
            await _botClient.SendTextMessageRequestAsync(tooLongPhrasesMessage, cancellationToken);
            
            return;
        }

        var channelUser = new ChannelUser(request.ChannelId, request.UserId, phrases);
        await _channelUserRepository.AddPhrases(channelUser, cancellationToken);

        _chatContextManager.OnPhrasesAdded(request.UserId, request.ChannelId);

        var message = BotMessageBuilder.GetPhrasesWereAddedMessage(request.UserId, channel);
        await _botClient.SendTextMessageRequestAsync(message, cancellationToken);
        
    }
}
