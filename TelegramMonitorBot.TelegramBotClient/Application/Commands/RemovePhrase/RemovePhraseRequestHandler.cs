using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TelegramMonitorBot.Storage.Repositories.Abstractions;
using TelegramMonitorBot.TelegramBotClient.ChatContext;
using TelegramMonitorBot.TelegramBotClient.Extensions;
using TelegramMonitorBot.TelegramBotClient.Navigation;
using TelegramMonitorBot.TelegramBotClient.Navigation.Models;

namespace TelegramMonitorBot.TelegramBotClient.Application.Commands.RemovePhrase;

public class RemovePhraseRequestHandler : IRequestHandler<RemovePhraseRequest>
{
    private readonly ITelegramBotClient _botClient;
    private readonly IChannelUserRepository _channelUserRepository;
    private readonly BotNavigationManager _botNavigationManager;
    private readonly ChatContextManager _chatContextManager;

    public RemovePhraseRequestHandler(
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

    public async Task Handle(RemovePhraseRequest request, CancellationToken cancellationToken)
    {
        var chatId = request.CallbackQuery.Message!.Chat.Id;
        var channelId = request.ChannelId;
        
        await _channelUserRepository.RemovePhrase(channelId, chatId, request.Phrase, cancellationToken);
        await _botClient.AnswerCallbackQueryAsync(request.CallbackQuery.Id, $"Фраза \"{request.Phrase}\" удалена", cancellationToken: cancellationToken);
        
        var channel = await _channelUserRepository.GetChannel(channelId, cancellationToken);
        var phrases = await _channelUserRepository.GetChannelUserPhrases(channelId, chatId, cancellationToken);

        MessageRequest messageRequest;
        if (phrases.Count > 0)
        {
            messageRequest = _botNavigationManager.GetChannelPhrasesRequest(request.CallbackQuery.Message!.Chat.Id, channel, phrases, 1);
        }
        else
        {
            var channels = await _channelUserRepository.GetChannels(chatId, null, cancellationToken);
            messageRequest = _botNavigationManager.GetMyChannelsMessageRequest(request.CallbackQuery.Message!.Chat.Id, channels);
        }

        await _botClient.SendTextMessageRequestAsync(messageRequest, cancellationToken);
        _chatContextManager.OnPhraseRemoved(chatId);
    }
}