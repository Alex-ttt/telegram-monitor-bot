using Telegram.Bot.Types.ReplyMarkups;
using TelegramMonitorBot.Domain.Models;
using TelegramMonitorBot.Storage.Repositories.Abstractions.Models;
using TelegramMonitorBot.TelegramBotClient.Application.Services;
using TelegramMonitorBot.TelegramBotClient.Navigation.Models;

namespace TelegramMonitorBot.TelegramBotClient.Navigation;

public class BotNavigationManager
{
    public MessageRequest GetMyChannelsMessageRequest(long chatId, PageResult<Channel> channels)
    {
        if (channels.Any() is false)
        {
            return new MessageRequest(chatId,"У вас ещё нет каналов");
        }

        const string hammerAndWrench = "\ud83d\udee0"; // 🛠
        var buttons = channels
            .Select(t => new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData( t.Name + "\t" + hammerAndWrench, Routes.EditChannel(t.ChannelId)),
            })
            .ToList();

        var navigationButtons = new List<InlineKeyboardButton>();
        if (channels.PageNumber > 1)
        {
            navigationButtons.Add(InlineKeyboardButton.WithCallbackData("Назад", Routes.MyChannelsPage(channels.PageNumber - 1)));
        }
        
        if(channels.PagesCount > channels.PageNumber)
        {
            navigationButtons.Add(InlineKeyboardButton.WithCallbackData("Вперёд", Routes.MyChannelsPage(channels.PageNumber + 1)));
        }

        if (navigationButtons.Count != 0)
        {
            buttons.Add(navigationButtons);    
        }
        
        var keyboard = new InlineKeyboardMarkup(buttons);
        var request = new MessageRequest(chatId, "Полный список каналов, на которые вы подписаны", keyboard);

        return request;
    }

    public MessageRequest GetChannelPhrasesRequest(long chatId, Channel? channel, ICollection<string> phrases, int page)
    {
        if (channel is null)
        {
            return ChannelNotFound(chatId);
        }

        var channelId = channel.ChannelId;
        if (phrases.Count is 0)
        {
            var keyboard = new InlineKeyboardMarkup(
                new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData("Добавить фразы", Routes.AddPhrases(channelId))},
                    new[] { InlineKeyboardButton.WithCallbackData("Вернуться к моим каналам", Routes.MyChannels)}
                });
            
            return new MessageRequest(chatId, "В данный канал еще не были добавлены фразы", keyboard);
        }

        var pager = ChannelService.GetDefaultPhrasesListPager(page);
        var pageResult = new PageResult<string>(phrases, pager);

        var phrasesKeyboardButtons = pageResult
            .Select(t => new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(t, Routes.PhraseIgnore), 
                InlineKeyboardButton.WithCallbackData("Удалить", Routes.RemovePhrase(channelId, t))
            })
            .ToList();
        
        phrasesKeyboardButtons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData("Вернуться к каналу", Routes.EditChannel(channelId))
        });

        var additionalButtons = new List<InlineKeyboardButton>();
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

        if (additionalButtons.Count is > 0)
        {
            phrasesKeyboardButtons.Add(additionalButtons);
        }

        var phrasesKeyboard = new InlineKeyboardMarkup(phrasesKeyboardButtons);
        return new MessageRequest(chatId, $"Список фраз для канала @{channel.Name}", phrasesKeyboard);
    }

    public MessageRequest ChannelNotFound(long chatId)
    {
        return new MessageRequest(chatId, "Канал не найден");
    }

    public MessageRequest AlreadySubscribed(long chatId, string channelName)
    {
        return new MessageRequest(chatId, $"Подписка на канал @{channelName} уже существует");
    }
    
    public MessageRequest ChannelAdded(long chatId, string channelName)
    {
        var keyboardMarkup = new InlineKeyboardMarkup(
            new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData($"Добавить другой канал", Routes.Subscribe)},
                new[] { InlineKeyboardButton.WithCallbackData("К списку каналов", Routes.MyChannels)}
            });
        return new MessageRequest(chatId, $"Канал @{channelName} добавлен", keyboardMarkup);
    }

    public MessageRequest AskUnsubscribeFromChannel(long chatId, long channelId, string channelName)
    {
        var keyboardMarkup = new InlineKeyboardMarkup(
            new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData($"Отписаться от канала @{channelName}", Routes.AcceptUnsubscribeFrom(channelId))},
                new[] { InlineKeyboardButton.WithCallbackData("Отменить", Routes.MyChannels)}
            });
        
        return new MessageRequest(
            chatId,
            $"Вы уверены, что хотите отписаться от канала @{channelName}? \nВсе добавленные фразы для канала будут удалены безвозвратно.",
            keyboardMarkup);
    }
}