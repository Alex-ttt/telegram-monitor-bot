using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramMonitorBot.Domain.Models;
using TelegramMonitorBot.Storage.Repositories.Abstractions;
using TelegramMonitorBot.TelegramBotClient.ChatContext;

namespace TelegramMonitorBot.TelegramBotClient.Application.Commands.AddPhrasesToChannel;

public class AddPhrasesToChannelRequestHandler : IRequestHandler<AddPhrasesToChannelRequest>
{
    private readonly ITelegramBotClient _botClient;
    private readonly ITelegramRepository _telegramRepository;
    private readonly ChatContextManager _chatContextManager;

    public AddPhrasesToChannelRequestHandler(
        ITelegramBotClient botClient, 
        ITelegramRepository telegramRepository, 
        ChatContextManager chatContextManager)
    {
        _botClient = botClient;
        _telegramRepository = telegramRepository;
        _chatContextManager = chatContextManager;
    }

    public async Task Handle(AddPhrasesToChannelRequest request, CancellationToken cancellationToken)
    {
        var channel = await _telegramRepository.GetChannel(request.ChannelId, cancellationToken);
        if (channel is null)
        {
            await _botClient.SendTextMessageAsync(
                request.UserId,
                "Канал не найден",
                cancellationToken: cancellationToken);
            
            return;
        }
        
        var phrases = request.Phrases
            .Select(t => t.Trim())
            .Where(t => t.Length > 0)
            .Distinct()
            .ToList();

        // TODO adjust len
        if (phrases.Any(t => t.Length > 30))
        {
            await _botClient.SendTextMessageAsync(request.UserId, "Длина каждой фразы не должна превышать 30 символов", cancellationToken: cancellationToken);
            return;
        }

        var channelUser = new ChannelUser(request.ChannelId, request.UserId, phrases);
        await _telegramRepository.AddPhrases(channelUser, cancellationToken);

        var keyboard = new InlineKeyboardMarkup(
            new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("Добавить другие фразы", $"/add_phrases_to_{channel.ChannelId}"), },
                new[] { InlineKeyboardButton.WithCallbackData("Назад к моим каналам", "/my_channels"), },
            });
        
        await _botClient.SendTextMessageAsync(request.UserId, "Фразы успешно добавлены", replyMarkup: keyboard, cancellationToken: cancellationToken);
        
        _chatContextManager.OnPhrasesAdded(request.UserId, request.ChannelId);
    }
}