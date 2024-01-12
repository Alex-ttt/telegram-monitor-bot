using Telegram.Bot.Types.ReplyMarkups;
using TelegramMonitorBot.Domain.Models;
using TelegramMonitorBot.Storage.Repositories.Abstractions.Models;
using TelegramMonitorBot.TelegramBotClient.Application.Services;
using TelegramMonitorBot.TelegramBotClient.Navigation.Models;

namespace TelegramMonitorBot.TelegramBotClient.Navigation;

public static class BotMessageBuilder
{
    public static MessageRequest GetMyChannelsMessageRequest(long chatId, PageResult<Channel> channels)
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

    public static MessageRequest GetChannelPhrasesRequest(long chatId, Channel? channel, ICollection<string> phrases, int page)
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

    public static MessageRequest ChannelNotFound(long chatId)
    {
        return new MessageRequest(chatId, "Канал не найден.");
    }
    

    public static MessageRequest AlreadySubscribed(long chatId, string channelName)
    {
        return new MessageRequest(chatId, $"Подписка на канал @{channelName} уже существует");
    }
    
    public static MessageRequest ChannelAdded(long chatId, string channelName)
    {
        var keyboardMarkup = new InlineKeyboardMarkup(
            new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData($"Добавить другой канал", Routes.Subscribe)},
                new[] { InlineKeyboardButton.WithCallbackData("К списку каналов", Routes.MyChannels)}
            });
        return new MessageRequest(chatId, $"Канал @{channelName} добавлен", keyboardMarkup);
    }

    public static MessageRequest AskUnsubscribeFromChannel(long chatId, long channelId, string channelName)
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

    public static MessageRequest GetAbout(long chatId)
    {
        return new MessageRequest(
            chatId,
            "Этот бот создан для того, чтобы помочь отслеживать появление ключевых слов в публичных каналах телеграма");
    }

    public static MessageRequest GetInputChannelNameMessage(long chatId)
    {
        return new MessageRequest(
            chatId,
            "Введите короткие имя (начинается с @) или ссылку на канал, чтобы подписаться на него");
    }

    public static MessageRequest GetChannelSettingsMessage(long chatId, Channel channel)
    {
        var buttons = new[]
        {
            new[] { new InlineKeyboardButton("Добавить фразы для поиска") { CallbackData = Routes.AddPhrases(channel.ChannelId)}},
            new[] { new InlineKeyboardButton("Список фраз для поиска") { CallbackData = Routes.ShowChannelPhrases(channel.ChannelId)}},
            new[] { new InlineKeyboardButton("Отписаться") { CallbackData = Routes.UnsubscribeFrom(channel.ChannelId)}},
            new[] { new InlineKeyboardButton("Перейти в канал") { Url = ChannelService.ChannelLink(channel.Name) }},
            new[] { new InlineKeyboardButton("Назад") { CallbackData = Routes.MyChannels }},
        };
        
        var keyboard = new InlineKeyboardMarkup(buttons);

        return new MessageRequest(chatId, $"Настройка канала @{channel.Name}", keyboard);
    }

    public static MessageRequest GetUnrecognizedRequestMessage(long chatId)
    {
        return new MessageRequest(
            chatId,
            $"Не удалось распознать текущую команду. Используйте {Routes.Menu}, чтобы перейти в главное меню");
    }

    public static MessageRequest GetPhrasesWereAddedMessage(long chatId, Channel channel)
    {
        var keyboard = new InlineKeyboardMarkup(
            new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("Добавить другие фразы", Routes.AddPhrases(channel.ChannelId)), },
                new[] { InlineKeyboardButton.WithCallbackData($"Назад к {channel.Name}", Routes.EditChannel(channel.ChannelId)), },
                new[] { InlineKeyboardButton.WithCallbackData("Назад к моим каналам", Routes.MyChannels), },
            });
        
        return new MessageRequest(chatId, "Фразы успешно добавлены", keyboard);
    }

    public static MessageRequest GetPhrasesTooLongMessage(long chatId, int lenLimit)
    {
        return new MessageRequest(chatId, $"Длина каждой фразы не должна превышать {lenLimit} символов");
    }

    public static MessageRequest GetMenuMessage(long chatId)
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("Мои каналы", Routes.MyChannels), },
            new[] { InlineKeyboardButton.WithCallbackData("Подписаться", Routes.Subscribe), },
        });

        return new MessageRequest(chatId, "Управление каналами", inlineKeyboard);
    }

    public static MessageRequest GetPrepareChannelsForPhraseAddingMessage(long chatId, Channel channel)
    {
        var keyboardMarkup = new InlineKeyboardMarkup(
            new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("Назад к моим каналам", Routes.MyChannels), },
            });

        return new MessageRequest(
            chatId,
            $"Введите фразы для поиска по каналу @{channel.Name}. Можно написать несколько фраз - каждую с новой строки",
            keyboardMarkup);
    }
}