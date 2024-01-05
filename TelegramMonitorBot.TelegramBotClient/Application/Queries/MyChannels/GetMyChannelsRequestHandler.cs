using MediatR;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramMonitorBot.Storage.Repositories.Abstractions;
using TelegramMonitorBot.TelegramApiClient;
using TelegramMonitorBot.TelegramBotClient.Application.Services;
using TelegramMonitorBot.TelegramBotClient.ChatContext;
using TelegramMonitorBot.TelegramBotClient.Services;

namespace TelegramMonitorBot.TelegramBotClient.Application.Queries.MyChannels;

public class GetMyChannelsRequestHandler : IRequestHandler<GetMyChannelsRequest, GetMyChannelsResponse>
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<UpdateHandler> _logger;
    private readonly ChatContextManager _chatContextManager;
    private readonly ITelegramRepository _telegramRepository;
    private readonly ITelegramApiClient _telegramApiClient;

    public GetMyChannelsRequestHandler(
        ITelegramBotClient botClient, 
        ILogger<UpdateHandler> logger, 
        ChatContextManager chatContextManager, 
        ITelegramRepository telegramRepository, 
        ITelegramApiClient telegramApiClient)
    {
        _botClient = botClient;
        _logger = logger;
        _chatContextManager = chatContextManager;
        _telegramRepository = telegramRepository;
        _telegramApiClient = telegramApiClient;
    }

    public async Task<GetMyChannelsResponse> Handle(GetMyChannelsRequest request, CancellationToken cancellationToken)
    {
        var message = request.CallbackQuery.Message;
        var channels = await _telegramRepository.GetChannels(message.Chat.Id, pager ?? GetDefaultChannelsPager(1), cancellationToken);

        if (channels.Any() is false)
        {
            return await _botClient.SendTextMessageAsync(
                message.Chat.Id,
                "У вас ещё нет каналов",
                cancellationToken: cancellationToken);;
        }

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

        var sent = await _botClient.SendTextMessageAsync(
            message.Chat.Id,
            "Полный список каналов, на которые вы подписаны",
            replyMarkup: keyboard, 
            cancellationToken: cancellationToken);

        return sent;
    }
}