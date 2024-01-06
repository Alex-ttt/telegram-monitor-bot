using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramMonitorBot.Domain.Models;
using TelegramMonitorBot.Storage.Repositories.Abstractions.Models;
using TelegramMonitorBot.TelegramBotClient.Application.Services;
using TelegramMonitorBot.TelegramBotClient.Navigation.Models;

namespace TelegramMonitorBot.TelegramBotClient.Navigation;

public class BotNavigationManager
{
    public MessageRequest GetMyChannelsMessageRequest(Message message, PageResult<Channel> channels)
    {
        if (channels.Any() is false)
        {
            return new MessageRequest(message.Chat.Id,"У вас ещё нет каналов");
        }

        var buttons = channels
            .Select(t => new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData( t.Name + "\t\ud83d\udee0", $"/edit_channel_{t.ChannelId}"),
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
        var request = new MessageRequest(message.Chat.Id, "Полный список каналов, на которые вы подписаны", keyboard);

        return request;
    }

    public MessageRequest GetChannelPhrasesRequest(Message message, Channel? channel, ICollection<string> phrases, int page)
    {
        if (channel is null)
        {
            return new MessageRequest(message.Chat.Id, "Канал не найден");
        }

        var channelId = channel.ChannelId;
        if (phrases.Count is 0)
        {
            var keyboard = new InlineKeyboardMarkup(
                new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData("Добавить фразы", $"/add_phrases_to_{channelId}")},
                    new[] { InlineKeyboardButton.WithCallbackData("Вернуться к моим каналам", "/my_channels")}
                });
            
            return new MessageRequest(message.Chat.Id, "В данный канал еще не были добавлены фразы", keyboard);
        }

        var pager = ChannelService.GetDefaultPhrasesListPager(page);
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
        return new MessageRequest(message.Chat.Id, $"Список фраз для канала @{channel.Name}", phrasesKeyboard);
    }
}