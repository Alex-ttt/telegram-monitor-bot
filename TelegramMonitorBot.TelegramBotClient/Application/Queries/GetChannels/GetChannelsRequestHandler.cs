using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramMonitorBot.Domain.Models;
using TelegramMonitorBot.Storage.Repositories.Abstractions;
using TelegramMonitorBot.Storage.Repositories.Abstractions.Models;
using TelegramMonitorBot.TelegramBotClient.Application.Services;

namespace TelegramMonitorBot.TelegramBotClient.Application.Queries.GetChannels;

public class GetChannelsRequestHandler : IRequestHandler<GetChannelsRequest>
{
    private readonly ITelegramBotClient _botClient;
    private readonly ITelegramRepository _telegramRepository;

    public GetChannelsRequestHandler(
        ITelegramBotClient botClient, 
        ITelegramRepository telegramRepository)
    {
        _botClient = botClient;
        _telegramRepository = telegramRepository;
    }

    public async Task Handle(GetChannelsRequest request, CancellationToken cancellationToken)
    {
        if (request.CallbackQuery.Message is not { } message)
        {
            throw new ArgumentNullException(nameof(request.CallbackQuery.Message), "Message on callback query can't be null");
        }
        
        var channels = await GetChannels(request, cancellationToken);
        
        if (channels.Any() is false)
        {
            await SendResponseNoChannels(message.Chat.Id, cancellationToken: cancellationToken);
            return;
        }
        
        var keyboard = CreateMarkup(channels);
        
        await _botClient.SendTextMessageAsync(
            message.Chat.Id,
            "Полный список каналов, на которые вы подписаны",
            replyMarkup: keyboard, 
            cancellationToken: cancellationToken);
    }

    private static InlineKeyboardMarkup CreateMarkup(PageResult<Channel> channels)
    {
        var buttons = channels
            .Select(t => new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithUrl(t.Name, ChannelService.ChannelLink(t.Name)),
                new InlineKeyboardButton("Настроить")
                {
                    CallbackData = $"/edit_channel_{t.ChannelId}",
                },
            })
            .ToList();
        
        var navigationButtons = new List<InlineKeyboardButton>();
        if (channels.PageNumber > 1)
        {
            navigationButtons.Add(InlineKeyboardButton.WithCallbackData("Назад", $"/my_channels_{channels.PageNumber - 1}"));
        }
        
        if(channels.PagesCount > channels.PageNumber)
        {
            navigationButtons.Add(InlineKeyboardButton.WithCallbackData("Вперёд", $"/my_channels_{channels.PageNumber + 1}"));
        }
        
        if (navigationButtons.Count != 0)
        {
            buttons.Add(navigationButtons);    
        }
        
        var keyboard = new InlineKeyboardMarkup(buttons);
        return keyboard;
    }

    private async Task<PageResult<Channel>> GetChannels(GetChannelsRequest request, CancellationToken cancellationToken)
    {
        var channelsPager = ChannelService.GetDefaultChannelsListPager(request.Page);
        var channels = 
            await _telegramRepository.GetChannels(request.CallbackQuery.Message!.Chat.Id, channelsPager, cancellationToken);

        return channels;
    }

    // TODO To Common handler
    private async Task SendResponseNoChannels(long chatId, CancellationToken cancellationToken)
    {
        await _botClient.SendTextMessageAsync(
            chatId,
            "У вас ещё нет каналов",
            cancellationToken: cancellationToken);
    }
}