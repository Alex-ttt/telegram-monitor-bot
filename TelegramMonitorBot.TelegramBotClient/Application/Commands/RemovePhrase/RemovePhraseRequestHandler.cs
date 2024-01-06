using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TelegramMonitorBot.Storage.Repositories.Abstractions;
using TelegramMonitorBot.TelegramBotClient.Extensions;
using TelegramMonitorBot.TelegramBotClient.Navigation;

namespace TelegramMonitorBot.TelegramBotClient.Application.Commands.RemovePhrase;

public class RemovePhraseRequestHandler : IRequestHandler<RemovePhraseRequest>
{
    private readonly ITelegramBotClient _botClient;
    private readonly ITelegramRepository _telegramRepository;
    private readonly BotNavigationManager _botNavigationManager;

    public RemovePhraseRequestHandler(ITelegramBotClient botClient, ITelegramRepository telegramRepository, BotNavigationManager botNavigationManager)
    {
        _botClient = botClient;
        _telegramRepository = telegramRepository;
        _botNavigationManager = botNavigationManager;
    }

    public async Task Handle(RemovePhraseRequest request, CancellationToken cancellationToken)
    {
        var chatId = request.CallbackQuery.Message!.Chat.Id;
        var channelId = request.ChannelId;
        
        await _telegramRepository.RemovePhrase(channelId, chatId, request.Phrase, cancellationToken);
        await _botClient.AnswerCallbackQueryAsync(request.CallbackQuery.Id, $"Фраза \"{request.Phrase}\" удалена", cancellationToken: cancellationToken);
        
        await Task.Delay(500, cancellationToken);
        await _botClient.SendChatActionAsync(chatId, ChatAction.Typing, cancellationToken: cancellationToken);
        
        var channel = await _telegramRepository.GetChannel(channelId, cancellationToken);
        var phrases = await _telegramRepository.GetChannelUserPhrases(channelId, chatId, cancellationToken);

        var messageRequest = _botNavigationManager.GetChannelPhrasesRequest(request.CallbackQuery.Message!, channel, phrases, 1);
        await _botClient.SendTextMessageRequestAsync(messageRequest, cancellationToken);
    }
}