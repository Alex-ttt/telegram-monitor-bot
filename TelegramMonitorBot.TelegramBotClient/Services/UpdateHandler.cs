using System.Text.RegularExpressions;
using MediatR;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramMonitorBot.Domain.Models;
using TelegramMonitorBot.Storage.Repositories.Abstractions;
using TelegramMonitorBot.Storage.Repositories.Abstractions.Models;
using TelegramMonitorBot.TelegramApiClient;
using TelegramMonitorBot.TelegramBotClient.Application.Queries.MyChannels;
using TelegramMonitorBot.TelegramBotClient.ChatContext;

// ReSharper disable All

namespace TelegramMonitorBot.TelegramBotClient.Services;

public class UpdateHandler : IUpdateHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<UpdateHandler> _logger;
    private readonly ChatContextManager _chatContextManager;
    private readonly ITelegramRepository _telegramRepository;
    private readonly ITelegramApiClient _telegramApiClient;
    private readonly IMediator _mediator;
    
    private const string ChannelPagePlaceholder = "page";
    private static Regex ChannelPageRegex = new (@$"^\/my_channels(?<{ChannelPagePlaceholder}>_?\d+)?$", RegexOptions.Compiled);
    
    private const string EditChannelIdPlaceholder = "channelId";
    private static Regex EditChannelIdRegex = new (@$"^\/edit_channel_(?<{EditChannelIdPlaceholder}>-?\d+)$", RegexOptions.Compiled);

    private const string AddPhrasesChannelIdPlaceholder = "channelId";
    private static Regex AddPhrasesChannelIdRegex = new (@$"^\/add_phrases_to_(?<{AddPhrasesChannelIdPlaceholder}>-?\d+)$", RegexOptions.Compiled);
    
    private const string RemovePhrasesChannelIdPlaceholder = "channelId";
    private const string RemovePhrasesPagePlaceholder = "page";
    private static Regex  RemovePhrasesChannelIdRegex = new (@$"^\/remove_phrases_from_(?<{RemovePhrasesChannelIdPlaceholder}>-?\d+)(?<{RemovePhrasesPagePlaceholder}>_\d+)?$", RegexOptions.Compiled);

    private const string RemovePrecisePhraseChannelIdPlaceholder = "channelId";
    private const string RemovePrecisePhrasePlaceholder = "phrase";
    private static Regex RemovePrecisePhraseRegex =
        new (@$"^\/remove_phrase_(?<{RemovePrecisePhraseChannelIdPlaceholder}>-?\d+)_(?<{RemovePrecisePhrasePlaceholder}>.+)$", RegexOptions.Compiled);

    private const string UnsubscribeChannelIdPlaceholder = "channelId";
    private static Regex UnsubscribeChannelIdRegex = new (@$"^\/unsubscribe_from_(?<{UnsubscribeChannelIdPlaceholder}>-?\d+)$", RegexOptions.Compiled);
    
    public UpdateHandler(
        ITelegramBotClient botClient, 
        ILogger<UpdateHandler> logger, 
        ChatContextManager chatContextManager, ITelegramRepository telegramRepository, ITelegramApiClient telegramApiClient, IMediator mediator)
    {
        _mediator = mediator;
        (_botClient, _chatContextManager, _telegramRepository, _telegramApiClient, _logger) = 
            (botClient, chatContextManager, telegramRepository, telegramApiClient, logger);
    }
    
    public async Task HandleUpdateAsync(ITelegramBotClient telegramBotClient, Update update, CancellationToken cancellationToken)
    {
        var handler = update switch
        {
            { Message: { } message} => BotOnMessageReceived(message, cancellationToken),
            { EditedMessage: { } message} => BotOnMessageReceived(message, cancellationToken),
            { CallbackQuery: { } callbackQuery} => BotOnCallbackQueryReceived(callbackQuery, cancellationToken),
            { InlineQuery: { } inlineQuery} => BotOnInlineQueryReceived(inlineQuery, cancellationToken),
            { ChosenInlineResult: { } chosenInlineResult} => BotOnChosenInlineResultReceived(chosenInlineResult, cancellationToken),
            
            _ => UnknownUpdateHandlerAsync(update, cancellationToken)
        };

        await handler;
    }

    private async Task BotOnMessageReceived(Message message, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetMyChannelsRequest(null!));
        _logger.LogInformation("Receive message type: {MessageType}", message.Type);
        if (message.Text is not { } messageText)
        {
            return;
        }

        var chatContext = _chatContextManager.GetCurrentContext(message.Chat.Id);
        if (chatContext.CurrentState == ChatState.WaitingForChannelToSubscribe)
        {
            await AddUserChannel(message, cancellationToken);
            return;
        }

        if (chatContext is {CurrentState: ChatState.WaitingForPhaseToAdd, ChannelId: { } channelId})
        {
            await AddPhasesToChannel(message, channelId, cancellationToken);
            return;
        }

        var firstWord = messageText.Split(' ')[0]; 
        var action = firstWord switch
        {
            "/about"     => SendAbout(message, cancellationToken),
            "/menu"     => MainMenu(message, cancellationToken),
            "/my_channels"     => MyChannels(message, null, cancellationToken),
            
            // "/inline_keyboard" => SendInlineKeyboard(_botClient, message, cancellationToken),
            "/keyboard"        => SendReplyKeyboard(_botClient, message, cancellationToken),
            // "/remove"          => RemoveKeyboard(_botClient, message, cancellationToken),
            // "/photo"           => SendFile(_botClient, message, cancellationToken),
            // "/request"         => RequestContactAndLocation(_botClient, message, cancellationToken),
            // "/inline_mode"     => StartInlineQuery(_botClient, message, cancellationToken),
            // "/throw"           => FailingHandler(_botClient, message, cancellationToken),
            _                  => Usage(_botClient, message, cancellationToken)
        };
        
        Message sentMessage = await action;
        _logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage.MessageId);

        async Task<Message> AddUserChannel(Message message, CancellationToken cancellationToken)
        {
            var context = _chatContextManager.GetCurrentContext(message.Chat.Id);
            var channel = await _telegramApiClient.FindChannelByName(message.Text!); // TODO think about null text
            if (channel is null)
            {
                return await _botClient.SendTextMessageAsync(
                    message.Chat.Id,
                    $"Не удалось найти канал {message.Text}",
                    cancellationToken: cancellationToken);
            }

            var alreadySubscribed = await _telegramRepository.CheckChannelWithUser(channel.Id, message.Chat.Id, cancellationToken);
            if (alreadySubscribed)
            {
                return await _botClient.SendTextMessageAsync(
                    message.Chat.Id,
                    $"Подписка на данный канал уже существует",
                    cancellationToken: cancellationToken);
            }

            var user = new Domain.Models.User(message.Chat.Id, message.Chat.Username ?? "_unknown_");

            await _telegramRepository.PutUserChannel(
                new(message.Chat.Id, message.Chat.Username ?? "_unknown_"),
                new(channel.Id, channel.Name),
                cancellationToken);

            _chatContextManager.OnAddedChannel(message.Chat.Id);

            return await _botClient.SendTextMessageAsync(
                message.Chat.Id,
                $"Канал @{channel.Name} добавлен",
                cancellationToken: cancellationToken);
        }
        
        async Task<Message> TextMessage(Message message, CancellationToken cancellationToken)
        {
            var context = _chatContextManager.GetCurrentContext(message.Chat.Id);
            if (context.CurrentState == ChatState.WaitingForChannelToSubscribe)
            {
                var channel = await _telegramApiClient.FindChannelByName(message.Text!); // TODO think about null text
                if (channel is null)
                {
                    return await _botClient.SendTextMessageAsync(
                        message.Chat.Id, 
                        $"Не удалось найти канал {message.Text}", 
                        cancellationToken: cancellationToken);
                }
                
                // _telegramRepository.PutUserChannel()
                

            // await _telegramRepository.AddUserToChannel(
                //     new User(message.Chat.Id, message.Chat.Username),
                //     new Channel(){ ChannelId = })
                
                _chatContextManager.OnAddedChannel(message.Chat.Id);
                
                return await _botClient.SendTextMessageAsync(
                    message.Chat.Id, 
                    $"Канал {message.Text} добавлен", 
                    cancellationToken: cancellationToken);
            }
            
            return await Usage(_botClient, message, cancellationToken);
        }
        
        async Task<Message> AddChannel(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            var sentMessage = await botClient.SendTextMessageAsync(
                message.Chat.Id,
                "Введите имя канала или ссылку на него",
                cancellationToken: cancellationToken);
            
            _chatContextManager.OnAddingChannel(message.Chat.Id);

            return sentMessage;
        }
        
        // Send inline keyboard
        // You can process responses in BotOnCallbackQueryReceived handler
        static async Task<Message> SendInlineKeyboard(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            await botClient.SendChatActionAsync(
                chatId: message.Chat.Id,
                chatAction: ChatAction.Typing,
                cancellationToken: cancellationToken);

            InlineKeyboardMarkup inlineKeyboard = new(
                new[]
                {
                    // first row
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("1.1", "11"),
                        InlineKeyboardButton.WithCallbackData("1.2", "12"),
                    },
                    // second row
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("2.1", "21"),
                        InlineKeyboardButton.WithCallbackData("2.2", "22"),
                    },
                });

            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Choose",
                replyMarkup: inlineKeyboard,
                cancellationToken: cancellationToken);
        }

        static async Task<Message> SendReplyKeyboard(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            
            var button = KeyboardButton.WithRequestChat("Select chat", new KeyboardButtonRequestChat
            {
                ChatIsChannel = true,

            });
            
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Games", "5")
                }
            });
            
            ReplyKeyboardMarkup replyKeyboardMarkup = new ReplyKeyboardMarkup(
                new[]
                {
                        new KeyboardButton[] 
                        { 
                            KeyboardButton.WithRequestChat("Select chat", new KeyboardButtonRequestChat
                            {
                                ChatIsChannel = true,
                                
                            })
                            
                        },
                        new KeyboardButton[] { "1.1", "1.2" },
                        new KeyboardButton[] { "2.1", "2.2" },
                })
            {
                ResizeKeyboard = true
            };

            var inlineButtons = new ReplyKeyboardMarkup(
                new []
                {
                    new KeyboardButton[] {"ReplyKeyboardMarkup"},
                    new KeyboardButton[] {"1.1", "1.2"},
                    new KeyboardButton[] {"2.1", "2.2"},
                });
            
            var m1 = await botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: "InlineKeyboardMarkup",
                            replyMarkup: inlineKeyboard,
                            cancellationToken: cancellationToken);
            
            var m2 = await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "ForceReplyMarkup",
                replyMarkup: new ForceReplyMarkup()
                ,
                cancellationToken: cancellationToken);
            
            var m3 = await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "ReplyKeyboardMarkup",
                replyMarkup: replyKeyboardMarkup,
                cancellationToken: cancellationToken);

            return m3;

        }

        static async Task<Message> RemoveKeyboard(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Removing keyboard",
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken);
        }

        static async Task<Message> SendFile(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            await botClient.SendChatActionAsync(
                message.Chat.Id,
                ChatAction.UploadPhoto,
                cancellationToken: cancellationToken);

            const string filePath = "Files/tux.png";
            await using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var fileName = filePath.Split(Path.DirectorySeparatorChar).Last();

            return await botClient.SendPhotoAsync(
                chatId: message.Chat.Id,
                photo: new InputFileStream(fileStream, fileName),
                caption: "Nice Picture",
                cancellationToken: cancellationToken);
        }

        static async Task<Message> RequestContactAndLocation(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            var requestReplyKeyboard = new ReplyKeyboardMarkup(
                new[]
                {
                    KeyboardButton.WithRequestLocation("Location"),
                    KeyboardButton.WithRequestContact("Contact"),
                });

            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Who or Where are you?",
                replyMarkup: requestReplyKeyboard,
                cancellationToken: cancellationToken);
        }

        static async Task<Message> Usage(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            const string usage = "Usage:\n" +
                                 "/inline_keyboard - send inline keyboard\n" +
                                 "/keyboard    - send custom keyboard\n" +
                                 "/remove      - remove custom keyboard\n" +
                                 "/photo       - send a photo\n" +
                                 "/request     - request location or contact\n" +
                                 "/get_subscribtions - get subscribtions\n" +
                                 "/inline_mode - send keyboard with Inline Query";

            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: usage,
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken);
        }

        static async Task<Message> StartInlineQuery(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            InlineKeyboardMarkup inlineKeyboard = new(
                InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Inline Mode"));

            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Press the button to start Inline Query",
                replyMarkup: inlineKeyboard,
                cancellationToken: cancellationToken);
        }


        static Task<Message> FailingHandler(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            throw new IndexOutOfRangeException();
        }
    }

    private async Task AddPhasesToChannel(Message message, long channelId, CancellationToken cancellationToken)
    {
        var channel = await _telegramRepository.GetChannel(channelId, cancellationToken);
        if (channel is null)
        {
            await _botClient.SendTextMessageAsync(
                message.Chat.Id,
                "Канал не найден");
        }

        var phrases = message.Text!
            .Split("\n")
            .Select(t => t.Trim())
            .Distinct()
            .ToList();

        if (phrases.Any(t => t.Length > 40))
        {
            await _botClient.SendTextMessageAsync(
                message.Chat.Id,
                "Длина одной фразы не должна превышать 50 символов");
            
            return;
        }

        var userId = message.Chat.Id;
        var channelUser = new ChannelUser(channel!.ChannelId, userId, phrases);
        await _telegramRepository.AddPhrases(channelUser, cancellationToken);

        var keyboard = new InlineKeyboardMarkup(
            new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("Добавить другие фразы", $"/add_phrases_to_{channel.ChannelId}"), },
                new[] { InlineKeyboardButton.WithCallbackData("Назад к моим каналам", "/my_channels"), },
            });
        
        await _botClient.SendTextMessageAsync(
            message.Chat.Id,
            "Фразы успешно добавлены",
            replyMarkup: keyboard);
        
        _chatContextManager.OnPhrasesAdded(userId, channelId);
    }

    private async Task<Message> MainMenu(Message message, CancellationToken cancellationToken)
    {
        
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Мои каналы", "/my_channels"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Подписаться", "/subscribe"),
            },
        });
        
        return await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Управление каналами",
            replyMarkup: inlineKeyboard,
            cancellationToken: cancellationToken);
    }

    private Task<Message> SendAbout(Message message, CancellationToken cancellationToken)
    {
        return _botClient.SendTextMessageAsync(
            message.Chat.Id,
            "Бот для подписки на каналы телеграм",
            cancellationToken: cancellationToken);
    }

    private async Task<Message> MyChannels(Message message, Pager? pager, CancellationToken cancellationToken)
    {
        PageResult<Channel> channels = await _telegramRepository.GetChannels(message.Chat.Id, pager ?? GetDefaultChannelsPager(1), cancellationToken);

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
                InlineKeyboardButton.WithUrl(t.Name, ChannelLink(t.Name)),
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
            replyMarkup: keyboard);

        return sent;
    }

    private static string ChannelLink(string channelName) => $@"https://t.me/{channelName}";

    // Process Inline Keyboard callback data
    private async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received inline keyboard callback from: {CallbackQueryId}", callbackQuery.Id);
        
        var channelPageMatch = ChannelPageRegex.Match(callbackQuery.Data);
        if (channelPageMatch.Success)
        {
            Pager? pager = null;
            if (channelPageMatch.Groups[ChannelPagePlaceholder] is { Success: true} page)
            {
                pager = GetDefaultChannelsPager(int.Parse(page.Value));
            }
            
            await MyChannels(callbackQuery.Message!, pager, cancellationToken);
            return;
        }

        var editChannelMatch = EditChannelIdRegex.Match(callbackQuery.Data);
        if (editChannelMatch.Success)
        {
            var channelIdString = editChannelMatch.Groups[EditChannelIdPlaceholder].Value;
            var channelId = long.Parse(channelIdString);
            await EditChannel(callbackQuery.Message!, channelId, cancellationToken);
            return;
        }
        
        var addPhrasesMatch = AddPhrasesChannelIdRegex.Match(callbackQuery.Data);
        if (addPhrasesMatch.Success)
        {
            var channelIdString = addPhrasesMatch.Groups[AddPhrasesChannelIdPlaceholder].Value;
            var channelId = long.Parse(channelIdString);
            await PrepareForAddingPhrases(callbackQuery.Message!, channelId, cancellationToken);
            return;
        }
        
        var removePhrasesMathc = RemovePhrasesChannelIdRegex.Match(callbackQuery.Data);
        if(removePhrasesMathc.Success)
        {
            var channelIdString = removePhrasesMathc.Groups[RemovePhrasesChannelIdPlaceholder].Value;
            var channelId = long.Parse(channelIdString);
            var page = 1;
            if (removePhrasesMathc.Groups[RemovePhrasesPagePlaceholder] is {Success: true} pageGroup)
            {
                // we need substring to have simplier regex 
                // math starts with "_"
                page = int.Parse(pageGroup.Value.Substring(1));
            }
            
            await ShowChannelPhrases(callbackQuery.Message!, channelId, page, cancellationToken);
            return;
        }


        var removePrecisePhraseMatch = RemovePrecisePhraseRegex.Match(callbackQuery.Data);
        if (removePrecisePhraseMatch.Success)
        {
            var channelIdString = removePrecisePhraseMatch.Groups[RemovePrecisePhraseChannelIdPlaceholder].Value;
            var channelId = long.Parse(channelIdString);
            var phrase = removePrecisePhraseMatch.Groups[RemovePrecisePhrasePlaceholder].Value;
            await RemovePrecisePhrase(callbackQuery, channelId, phrase, cancellationToken);
            return;
        }
        
        var unsubscribeFromChannelMatch = UnsubscribeChannelIdRegex.Match(callbackQuery.Data);
        if (unsubscribeFromChannelMatch.Success)
        {
            var channelIdString = unsubscribeFromChannelMatch.Groups[UnsubscribeChannelIdPlaceholder].Value;
            var channelId = long.Parse(channelIdString);
            await Unsubscribe(callbackQuery, channelId, cancellationToken);
            return;
        }
        
        if (callbackQuery.Data == "/subscribe")
        {
            await Subscribe(callbackQuery.Message!, cancellationToken);
            return;
        }
        
        await _botClient.AnswerCallbackQueryAsync(
            callbackQueryId: callbackQuery.Id,
            text: $"Received {callbackQuery.Data}",
            cancellationToken: cancellationToken);

        await _botClient.SendTextMessageAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            text: $"Received {callbackQuery.Data}",
            cancellationToken: cancellationToken);
    }

    private async Task Unsubscribe(CallbackQuery callbackQuery, long channelId, CancellationToken cancellationToken)
    {
        var message = callbackQuery.Message!;
        await _telegramRepository.RemoveChannelUser(channelId, message.Chat.Id, cancellationToken);
        
        await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, $"Вы успешно отписались от канала", cancellationToken: cancellationToken);
        await MyChannels(message, null, cancellationToken);
    }

    private async Task RemovePrecisePhrase(CallbackQuery callbackQuery, long channelId, string phrase, CancellationToken cancellationToken)
    {
        await _telegramRepository.RemovePhrase(channelId, callbackQuery.Message!.Chat.Id, phrase, cancellationToken);
        await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, $"Фраза \"{phrase}\" удалена", cancellationToken: cancellationToken);
        await Task.Delay(500, cancellationToken);
        await _botClient.SendChatActionAsync(callbackQuery.Message!.Chat.Id, ChatAction.Typing, cancellationToken: cancellationToken);
        
        await ShowChannelPhrases(callbackQuery.Message, channelId, 1, cancellationToken);
    }

    private async Task ShowChannelPhrases(Message callbackQueryMessage, long channelId, int page, CancellationToken cancellationToken)
    {
        var phrases = await _telegramRepository.GetChannelUserPhrases(channelId, callbackQueryMessage.Chat.Id, cancellationToken);
        if (phrases.Count is 0)
        {
            var keyboard = new InlineKeyboardMarkup(
                new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData("Добавить фразы", $"/add_phrases_to_{channelId}")},
                    new[] { InlineKeyboardButton.WithCallbackData("Вернуться к моим каналам", "/my_channels")}
                });
            
            await _botClient.SendTextMessageAsync(
                callbackQueryMessage.Chat.Id,
                "В данный канал еще не были добавлены фразы",
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
            
            return;
        }
        
        var channel = await _telegramRepository.GetChannel(channelId, cancellationToken);
        if (channel is null)
        {
            await _botClient.SendTextMessageAsync(
                callbackQueryMessage.Chat.Id,
                "Канал не найден",
                cancellationToken: cancellationToken);
            
            return;
        }

        var pager = GetDefaultPhrasesPager(page);
        var pageResult = new PageResult<string>(phrases, pager);

        var phrasesKeyboardButtons = pageResult
            .Select(t => new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(t, "phrase_ignore"), 
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
                InlineKeyboardButton.WithCallbackData("Назад", $"/remove_phrases_from_{channelId}_{pager.Page - 1}"));
        }

        if (pageResult.PageNumber < pageResult.PagesCount)
        {
            additionalButtons.Add(
                InlineKeyboardButton.WithCallbackData("Вперёд", $"/remove_phrases_from_{channelId}_{pager.Page + 1}"));
        }

        if (additionalButtons.Count is > 0)
        {
            phrasesKeyboardButtons.Add(additionalButtons);
        }

        var phrasesKeyboard = new InlineKeyboardMarkup(phrasesKeyboardButtons);
        await _botClient.SendTextMessageAsync(
            callbackQueryMessage.Chat.Id,
            $"Список фраз для канала @{channel.Name}",
            replyMarkup: phrasesKeyboard,
            cancellationToken: cancellationToken);

    }

    private async Task PrepareForAddingPhrases(Message callbackQueryMessage, long channelId, CancellationToken cancellationToken)
    {
        var channel = await _telegramRepository.GetChannel(channelId, cancellationToken);
        if (channel is null)
        {
            await _botClient.SendTextMessageAsync(
                callbackQueryMessage.Chat.Id,
                "Канал не найден",
                cancellationToken: cancellationToken);
            
            return;
        }

        await _botClient.SendTextMessageAsync(
            callbackQueryMessage.Chat.Id,
            $"Введите фразы для поиска @{channel.Name}. Можно написать несколько фраз - каждую с новой строки");

        _chatContextManager.OnPhrasesAdding(callbackQueryMessage.Chat.Id, channel.ChannelId);

        // TODO handle context states

    }

    private async Task EditChannel(Message callbackQueryMessage, long channelId, CancellationToken cancellationToken)
    {
        var channel = await _telegramRepository.GetChannel(channelId, cancellationToken);
        if (channel is null)
        {
            await _botClient.SendTextMessageAsync(
                callbackQueryMessage.Chat.Id,
                "Канал не найден",
                cancellationToken: cancellationToken);
            return;
        }
        
        var buttons = new[]
        {
            new[] {new InlineKeyboardButton("Добавить фразы для поиска") { CallbackData = $"/add_phrases_to_{channelId}"}},
            new[] {new InlineKeyboardButton("Удалить фразы для поиска") { CallbackData = $"/remove_phrases_from_{channelId}"}},
            new[] {new InlineKeyboardButton("Отписаться") { CallbackData = $"/unsubscribe_from_{channelId}"}},
            new[] {new InlineKeyboardButton("Перейти в канал") { Url = ChannelLink(channel.Name) }},
            new[] {new InlineKeyboardButton("Назад") { CallbackData = "/my_channels" }},
        };
        
        var keyboard = new InlineKeyboardMarkup(buttons);
        
        await  _botClient.SendTextMessageAsync(
                callbackQueryMessage.Chat.Id,
            $"Настройка канала @{channel.Name}",
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
    }

    private async Task Subscribe(Message callbackQueryMessage, CancellationToken cancellationToken)
    {
        
        // var button = KeyboardButton.WithRequestChat("Выбрать канал", new KeyboardButtonRequestChat
        // {
        //     ChatIsChannel = true,
        //     RequestId = 108,
        // });
        //
        // var keyboard = new ReplyKeyboardMarkup(button);
        //
        
        await _botClient.SendTextMessageAsync(
            callbackQueryMessage.Chat.Id,
            "Введите короткие имя (начинается с @) или ссылку на канал, чтобы подписаться на него", 
            cancellationToken: cancellationToken);
        
        _chatContextManager.OnAddingChannel(callbackQueryMessage.Chat.Id);
    }

    #region Inline Mode

    private async Task BotOnInlineQueryReceived(InlineQuery inlineQuery, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received inline query from: {InlineQueryFromId}", inlineQuery.From.Id);

        InlineQueryResult[] results = {
            // displayed result
            new InlineQueryResultArticle(
                id: "1",
                title: "TgBots",
                inputMessageContent: new InputTextMessageContent("hello"))
        };

        await _botClient.AnswerInlineQueryAsync(
            inlineQueryId: inlineQuery.Id,
            results: results,
            cacheTime: 0,
            isPersonal: true,
            cancellationToken: cancellationToken);
    }

    private async Task BotOnChosenInlineResultReceived(ChosenInlineResult chosenInlineResult, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received inline result: {ChosenInlineResultId}", chosenInlineResult.ResultId);

        await _botClient.SendTextMessageAsync(
            chatId: chosenInlineResult.From.Id,
            text: $"You chose result with Id: {chosenInlineResult.ResultId}",
            cancellationToken: cancellationToken);
    }

    #endregion
    
    private Task UnknownUpdateHandlerAsync(Update update, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }

    public async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        _logger.LogInformation("HandleError: {ErrorMessage}", ErrorMessage);

        // Cooldown in case of network connection error
        if (exception is RequestException)
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }

    private Pager GetDefaultChannelsPager(int currentPage)
    {
        return new Pager(currentPage, 5); 
    } 
    
    private Pager GetDefaultPhrasesPager(int currentPage)
    {
        return new Pager(currentPage, 8); 
    } 
}
