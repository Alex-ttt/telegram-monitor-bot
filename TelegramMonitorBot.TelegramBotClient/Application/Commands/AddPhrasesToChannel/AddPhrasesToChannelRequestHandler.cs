using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
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
    private readonly BotNavigationManager _botNavigationManager;

    public AddPhrasesToChannelRequestHandler(
        ITelegramBotClient botClient, 
        IChannelUserRepository channelUserRepository, 
        ChatContextManager chatContextManager, BotNavigationManager botNavigationManager)
    {
        _botClient = botClient;
        _channelUserRepository = channelUserRepository;
        _chatContextManager = chatContextManager;
        _botNavigationManager = botNavigationManager;
    }

    public async Task Handle(AddPhrasesToChannelRequest request, CancellationToken cancellationToken)
    {
        var channel = await _channelUserRepository.GetChannel(request.ChannelId, cancellationToken);
        if (channel is null)
        {
            var channelNotFoundMessage = _botNavigationManager.ChannelNotFound(request.UserId);
            await _botClient.SendTextMessageRequestAsync(channelNotFoundMessage, cancellationToken);
            
            return;
        }
        
        var phrases = request.Phrases
            .Select(t => t.Trim())
            .Where(t => t.Length > 0)
            .Distinct()
            .ToList();
        
        if (phrases.Any(t => t.Length > 30))
        {
            await _botClient.SendTextMessageAsync(request.UserId, "Длина каждой фразы не должна превышать 30 символов", cancellationToken: cancellationToken);
            return;
        }

        var channelUser = new ChannelUser(request.ChannelId, request.UserId, phrases);
        await _channelUserRepository.AddPhrases(channelUser, cancellationToken);

        var keyboard = new InlineKeyboardMarkup(
            new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("Добавить другие фразы", Routes.AddPhrases(channel.ChannelId)), },
                new[] { InlineKeyboardButton.WithCallbackData($"Назад к {channel.Name}", Routes.EditChannel(channel.ChannelId)), },
                new[] { InlineKeyboardButton.WithCallbackData("Назад к моим каналам", Routes.MyChannels), },
            });
        
        await _botClient.SendTextMessageAsync(request.UserId, "Фразы успешно добавлены", replyMarkup: keyboard, cancellationToken: cancellationToken);
        
        _chatContextManager.OnPhrasesAdded(request.UserId, request.ChannelId);
    }
}