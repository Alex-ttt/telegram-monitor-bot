using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramMonitorBot.Storage.Repositories.Abstractions;
using TelegramMonitorBot.Storage.Repositories.Abstractions.Models;
using TelegramMonitorBot.TelegramBotClient.Application.Services;

namespace TelegramMonitorBot.TelegramBotClient.Application.Queries.GetChannelPhrases;

public class GetChannelPhrasesRequestHandler : IRequestHandler<GetChannelPhrasesRequest>
{
    private readonly ITelegramBotClient _botClient;
    private readonly IChannelUserRepository _channelUserRepository;

    public GetChannelPhrasesRequestHandler(ITelegramBotClient botClient, IChannelUserRepository channelUserRepository)
    {
        _botClient = botClient;
        _channelUserRepository = channelUserRepository;
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
            await SendChannelNotFoundMessage(chatId, cancellationToken);
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
                InlineKeyboardButton.WithCallbackData(t, "/phrase_ignore"),
                InlineKeyboardButton.WithCallbackData("Удалить", $"/remove_phrase_{channelId}_{t}")
            })
            .ToList();

        phrasesKeyboardButtons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData("Вернуться к каналу", $"/edit_channel_{channelId}")
        });

        var additionalButtons = new List<InlineKeyboardButton>();
        if (pager.Page > 1)
        {
            additionalButtons.Add(
                InlineKeyboardButton.WithCallbackData("Назад", $"/remove_phrases_{channelId}_{pager.Page - 1}"));
        }

        if (pageResult.PageNumber < pageResult.PagesCount)
        {
            additionalButtons.Add(
                InlineKeyboardButton.WithCallbackData("Вперёд", $"/remove_phrases_{channelId}_{pager.Page + 1}"));
        }

        if (additionalButtons.Count is > 0)
        {
            phrasesKeyboardButtons.Add(additionalButtons);
        }

        var phrasesKeyboard = new InlineKeyboardMarkup(phrasesKeyboardButtons);
        return phrasesKeyboard;
    }

    private Task SendChannelNotFoundMessage(long chatId, CancellationToken cancellationToken)
    {
        return _botClient.SendTextMessageAsync(
            chatId,
            "Канал не найден",
            cancellationToken: cancellationToken);
    }

    private async Task SendNoPhrasesInChannelMessage(long channelId, long chatId, CancellationToken cancellationToken)
    {
        var keyboard = new InlineKeyboardMarkup(
            new[]
            {
                new[] {InlineKeyboardButton.WithCallbackData("Добавить фразы", $"/add_phrases_to_{channelId}")},
                new[] {InlineKeyboardButton.WithCallbackData("Вернуться к моим каналам", "/my_channels")}
            });

        await _botClient.SendTextMessageAsync(
            chatId,
            "В данный канал еще не были добавлены фразы",
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);
    }
}