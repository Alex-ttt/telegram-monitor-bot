using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramMonitorBot.Storage.Repositories.Abstractions;
using TelegramMonitorBot.TelegramApiClient;
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
    
    public UpdateHandler(
        ITelegramBotClient botClient, 
        ILogger<UpdateHandler> logger, 
        ChatContextManager chatContextManager, ITelegramRepository telegramRepository, ITelegramApiClient telegramApiClient)
    {
        (_botClient, _chatContextManager, _telegramRepository, _telegramApiClient, _logger) = 
            (botClient, chatContextManager, telegramRepository, telegramApiClient, logger);
    }

    public async Task HandleUpdateAsync(ITelegramBotClient telegramBotClient, Update update, CancellationToken cancellationToken)
    {
        var handler = update switch
        {
            {Type: UpdateType.Message, Message: { } message} => BotOnMessageReceived(message, cancellationToken),
            // UpdateType.Unknown:
            // UpdateType.EditedChannelPost:
            // UpdateType.ShippingQuery:
            // UpdateType.PreCheckoutQuery:
            // UpdateType.Poll:
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
        _logger.LogInformation("Receive message type: {MessageType}", message.Type);
        if (message.Text is not { } messageText)
            return;
        
        // if (Math.PI > 1)
        // {
        //     await _botClient.SendChatActionAsync(
        //         chatId: message.Chat.Id,
        //         chatAction: ChatAction.Typing,
        //         cancellationToken: cancellationToken);
        //     
        //     await _botClient.SendDiceAsync(
        //         chatId: message.Chat.Id,
        //         emoji: Emoji.Dice,
        //         cancellationToken: cancellationToken);
        //     
        //     await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        //     
        //     var lastMsg = await _botClient.SendTextMessageAsync(
        //         message.Chat.Id,
        //         "Ксюша!",
        //         cancellationToken: cancellationToken);
        //     
        //     return;
        // }

        // var action = messageText.Split(' ')[0] switch
        // {
        //     "/inline_keyboard" => SendInlineKeyboard(_botClient, message, cancellationToken),
        //     "/keyboard"        => SendReplyKeyboard(_botClient, message, cancellationToken),
        //     "/remove"          => RemoveKeyboard(_botClient, message, cancellationToken),
        //     "/photo"           => SendFile(_botClient, message, cancellationToken),
        //     "/request"         => RequestContactAndLocation(_botClient, message, cancellationToken),
        //     "/inline_mode"     => StartInlineQuery(_botClient, message, cancellationToken),
        //     "/throw"           => FailingHandler(_botClient, message, cancellationToken),
        //     _                  => Usage(_botClient, message, cancellationToken)
        // };
        
        
        var action = messageText.Split(' ')[0] switch
        {
            "/add_channel"     => AddChannel(_botClient, message, cancellationToken),
            "/my_channels"     => MyChannels(_botClient, message, cancellationToken),
            "/keyboard"        => SendReplyKeyboard(_botClient, message, cancellationToken),
            "/remove"          => RemoveKeyboard(_botClient, message, cancellationToken),
            "/photo"           => SendFile(_botClient, message, cancellationToken),
            "/request"         => RequestContactAndLocation(_botClient, message, cancellationToken),
            "/inline_mode"     => StartInlineQuery(_botClient, message, cancellationToken),
            "/throw"           => FailingHandler(_botClient, message, cancellationToken),
            _                  => AddUserChannel(message, cancellationToken)
        };


        Message sentMessage = await action;
        _logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage.MessageId);

        // static async Task<Message> SendSomething(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        // {
        //     await botClient.SendChatActionAsync(
        //         chatId: message.Chat.Id,
        //         chatAction: ChatAction.Typing,
        //         cancellationToken: cancellationToken);
        //     
        //     
        // }

        async Task<Message> AddUserChannel(Message message, CancellationToken cancellationToken)
        {
            var currentState = _chatContextManager.GetCurrentState(message.Chat.Id);
            if (currentState == ChatState.WaitingForChannelToSubscribe || Math.E > 0)
            {
                var channel = await _telegramApiClient.FindChannelByName(message.Text!); // TODO think about null text
                if (channel is null)
                {
                    return await _botClient.SendTextMessageAsync(
                        message.Chat.Id, 
                        $"Не удалось найти канал {message.Text}", 
                        cancellationToken: cancellationToken);
                }

                var user = new Domain.Models.User(message.Chat.Id, message.Chat.Username ?? "_unknown_");
                
                await _telegramRepository.PutUserChannel(
                    new (message.Chat.Id, message.Chat.Username ?? "_unknown_"),
                    new (channel.Id, channel.Title),
                    cancellationToken);
                
                _chatContextManager.OnAddedChannel(message.Chat.Id);
                
                return await _botClient.SendTextMessageAsync(
                    message.Chat.Id, 
                    $"Канал {message.Text} добавлен", 
                    cancellationToken: cancellationToken);
            }
            
            return await Usage(_botClient, message, cancellationToken);
        }
        
        async Task<Message> TextMessage(Message message, CancellationToken cancellationToken)
        {
            var currentState = _chatContextManager.GetCurrentState(message.Chat.Id);
            if (currentState == ChatState.WaitingForChannelToSubscribe)
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
        
        async Task<Message> MyChannels(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            var channels = await _telegramRepository.GetChannels(message.Chat.Id, cancellationToken);

            if (channels.Any() is false)
            {
                return await botClient.SendTextMessageAsync(
                    message.Chat.Id,
                    "У вас ещё нет каналов",
                    cancellationToken: cancellationToken);;
            }
            
            return await botClient.SendTextMessageAsync(
                message.Chat.Id,
                $"Ваши каналы: {string.Join(", ", channels.Select(t => "@" + t.Name))}",
                cancellationToken: cancellationToken);;
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
            ReplyKeyboardMarkup replyKeyboardMarkup = new(
                new[]
                {
                        new KeyboardButton[] { "1.1", "1.2" },
                        new KeyboardButton[] { "2.1", "2.2" },
                })
            {
                ResizeKeyboard = true
            };

            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Choose",
                replyMarkup: replyKeyboardMarkup,
                cancellationToken: cancellationToken);
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

    // Process Inline Keyboard callback data
    private async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received inline keyboard callback from: {CallbackQueryId}", callbackQuery.Id);

        await _botClient.AnswerCallbackQueryAsync(
            callbackQueryId: callbackQuery.Id,
            text: $"Received {callbackQuery.Data}",
            cancellationToken: cancellationToken);

        await _botClient.SendTextMessageAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            text: $"Received {callbackQuery.Data}",
            cancellationToken: cancellationToken);
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
}
