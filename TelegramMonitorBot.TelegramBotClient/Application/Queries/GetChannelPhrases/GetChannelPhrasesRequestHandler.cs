using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramMonitorBot.Storage.Repositories.Abstractions;
using TelegramMonitorBot.Storage.Repositories.Abstractions.Models;
using TelegramMonitorBot.TelegramBotClient.Application.Services;
using TelegramMonitorBot.TelegramBotClient.Extensions;
using TelegramMonitorBot.TelegramBotClient.Navigation;

namespace TelegramMonitorBot.TelegramBotClient.Application.Queries.GetChannelPhrases;

public class GetChannelPhrasesRequestHandler : IRequestHandler<GetChannelPhrasesRequest>
{
    private readonly ITelegramBotClient _botClient;
    private readonly IChannelUserRepository _channelUserRepository;
    private readonly BotNavigationManager _botNavigationManager;

    public GetChannelPhrasesRequestHandler(
        ITelegramBotClient botClient,
        IChannelUserRepository channelUserRepository, 
        BotNavigationManager botNavigationManager)
    {
        _botClient = botClient;
        _channelUserRepository = channelUserRepository;
        _botNavigationManager = botNavigationManager;
    }

    public async Task Handle(GetChannelPhrasesRequest request, CancellationToken cancellationToken)
    {
        var channelId = request.ChannelId;
        var chatId = request.CallbackQuery.Message!.Chat.Id;
        var phrases = await _channelUserRepository.GetChannelUserPhrases(channelId, chatId, cancellationToken);
        if (phrases.Count is 0)
        {
            await SendNoPhrasesInChannelMessage(channelId, chatId, cancellationToken);
            return;
        }
        
        var channel = await _channelUserRepository.GetChannel(channelId, cancellationToken);
        if (channel is null)
        {
            var channelNotFoundMessage = _botNavigationManager.ChannelNotFound(chatId);
            await _botClient.SendTextMessageRequestAsync(channelNotFoundMessage, cancellationToken);

            return;
        }

        var phrasesKeyboard = GetPhrasesKeyboardMarkup(request, phrases, channelId);
        
        await _botClient.SendTextMessageAsync(
            chatId,
            $"Список фраз для канала @{channel.Name}",
            replyMarkup: phrasesKeyboard,
            cancellationToken: cancellationToken);

    }

    private static InlineKeyboardMarkup GetPhrasesKeyboardMarkup(GetChannelPhrasesRequest request, ICollection<string> phrases, long channelId)
    {
        var pager = ChannelService.GetDefaultPhrasesListPager(request.Page);
        var pageResult = new PageResult<string>(phrases, pager);

        var phrasesKeyboardButtons = pageResult
            .Select(t => new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(t, Routes.PhraseIgnore),
                InlineKeyboardButton.WithCallbackData("Удалить", Routes.RemovePhrase(channelId, t)),
            })
            .ToList();

        phrasesKeyboardButtons.AddRange(new List<InlineKeyboardButton>[]
        {
            new() { InlineKeyboardButton.WithCallbackData("Добавить другие фразы", Routes.AddPhrases(channelId))},
            new() { InlineKeyboardButton.WithCallbackData("Вернуться к каналу", Routes.EditChannel(channelId))},
        });

        var additionalButtons = new List<InlineKeyboardButton>
        {
            Capacity = 0
        };
        if (pager.Page > 1)
        {
            additionalButtons.Add(
                InlineKeyboardButton.WithCallbackData("Назад", Routes.ShowChannelPhrases(channelId, pager.Page - 1)));
        }

        if (pageResult.PageNumber < pageResult.PagesCount)
        {
            additionalButtons.Add(
                InlineKeyboardButton.WithCallbackData("Вперёд", Routes.ShowChannelPhrases(channelId, pager.Page + 1)));
        }

        if (additionalButtons.Count > 0)
        {
            phrasesKeyboardButtons.Add(additionalButtons);
        }

        var phrasesKeyboard = new InlineKeyboardMarkup(phrasesKeyboardButtons);
        return phrasesKeyboard;
    }

    private async Task SendNoPhrasesInChannelMessage(long channelId, long chatId, CancellationToken cancellationToken)
    {
        var keyboard = new InlineKeyboardMarkup(
            new[]
            {
                new[] {InlineKeyboardButton.WithCallbackData("Добавить фразы", Routes.AddPhrases(channelId))},
                new[] {InlineKeyboardButton.WithCallbackData("Вернуться к моим каналам", Routes.MyChannels)}
            });

        await _botClient.SendTextMessageAsync(
            chatId,
            "В данный канал еще не были добавлены фразы",
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);
    }
}