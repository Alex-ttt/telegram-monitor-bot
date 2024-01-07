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
using TelegramMonitorBot.TelegramBotClient.ChatContext;
using TelegramMonitorBot.TelegramBotClient.Routing;

// ReSharper disable All

namespace TelegramMonitorBot.TelegramBotClient.Services;

public class UpdateHandler : IUpdateHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<UpdateHandler> _logger;
    private readonly ChatContextManager _chatContextManager;
    private readonly IChannelUserRepository _channelUserRepository;
    private readonly ITelegramApiClient _telegramApiClient;
    private readonly IMediator _mediator;
    private readonly CallbackQueryRouter _callbackQueryRouter;
    private readonly MessageRouter _messageRouter;

    public UpdateHandler(
        ITelegramBotClient botClient,
        ILogger<UpdateHandler> logger,
        ChatContextManager chatContextManager, IChannelUserRepository channelUserRepository,
        ITelegramApiClient telegramApiClient, IMediator mediator, CallbackQueryRouter callbackQueryRouter,
        MessageRouter messageRouter)
    {
        _mediator = mediator;
        _callbackQueryRouter = callbackQueryRouter;
        _messageRouter = messageRouter;
        (_botClient, _chatContextManager, _channelUserRepository, _telegramApiClient, _logger) =
            (botClient, chatContextManager, channelUserRepository, telegramApiClient, logger);
    }

    public async Task HandleUpdateAsync(ITelegramBotClient telegramBotClient, Update update,
        CancellationToken cancellationToken)
    {
        var handler = update switch
        {
            { Message: { } message } => BotOnMessageReceived(message, cancellationToken),
            { EditedMessage: { } message } => BotOnMessageReceived(message, cancellationToken),
            { CallbackQuery: { } callbackQuery } => BotOnCallbackQueryReceived(callbackQuery, cancellationToken),
            { InlineQuery: { } inlineQuery } => BotOnInlineQueryReceived(inlineQuery, cancellationToken),
            { ChosenInlineResult: { } chosenInlineResult } => BotOnChosenInlineResultReceived(chosenInlineResult,
                cancellationToken),

            _ => UnknownUpdateHandlerAsync(update, cancellationToken)
        };

        await handler;
    }

    private async Task BotOnMessageReceived(Message message, CancellationToken cancellationToken)
    {
        // var response = await _mediator.Send(new GetMyChannelsRequest(null!));
        _logger.LogInformation("Receive message type: {MessageType}", message.Type);
        if (message.Text is not { } messageText)
        {
            return;
        }

        if (_messageRouter.RouteRequest(message) is { } routedRequest)
        {
            await _mediator.Send(routedRequest, cancellationToken);
            return;
        }

        var firstWord = messageText.Split(' ')[0];
        var action = firstWord switch
        {
            // "/inline_keyboard" => SendInlineKeyboard(_botClient, message, cancellationToken),
            "/keyboard" => SendReplyKeyboard(_botClient, message, cancellationToken),
            // "/remove"          => RemoveKeyboard(_botClient, message, cancellationToken),
            // "/photo"           => SendFile(_botClient, message, cancellationToken),
            // "/request"         => RequestContactAndLocation(_botClient, message, cancellationToken),
            // "/inline_mode"     => StartInlineQuery(_botClient, message, cancellationToken),
            // "/throw"           => FailingHandler(_botClient, message, cancellationToken),
            _ => Usage(_botClient, message, cancellationToken)
        };

        Message sentMessage = await action;
        _logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage.MessageId);
    }


    private Task<Message> SendAbout(Message message, CancellationToken cancellationToken)
    {
        return _botClient.SendTextMessageAsync(
            message.Chat.Id,
            "Бот для подписки на каналы телеграм",
            cancellationToken: cancellationToken);
    }

    private static string ChannelLink(string channelName) => $@"https://t.me/{channelName}";

    // Process Inline Keyboard callback data
    private async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received inline keyboard callback from: {CallbackQueryId}", callbackQuery.Id);

        if (_callbackQueryRouter.RouteRequest(callbackQuery) is { } request)
        {
            await _mediator.Send(request, cancellationToken);
            return;
        }

        await _botClient.AnswerCallbackQueryAsync(
            callbackQueryId: callbackQuery.Id,
            text: $"Received {callbackQuery.Data}",
            cancellationToken: cancellationToken);

        await _botClient.SendTextMessageAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            text: $"Unrouted callback message received {callbackQuery.Data}",
            cancellationToken: cancellationToken);
    }


    #region Inline Mode

    private async Task BotOnInlineQueryReceived(InlineQuery inlineQuery, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received inline query from: {InlineQueryFromId}", inlineQuery.From.Id);

        InlineQueryResult[] results =
        {
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

    private async Task BotOnChosenInlineResultReceived(ChosenInlineResult chosenInlineResult,
        CancellationToken cancellationToken)
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

    public async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException =>
                $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        _logger.LogInformation("HandleError: {ErrorMessage}", ErrorMessage);

        // Cooldown in case of network connection error
        if (exception is RequestException)
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }

    private static async Task<Message> SendReplyKeyboard(ITelegramBotClient botClient, Message message,
        CancellationToken cancellationToken)
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
            new[]
            {
                new KeyboardButton[] { "ReplyKeyboardMarkup" },
                new KeyboardButton[] { "1.1", "1.2" },
                new KeyboardButton[] { "2.1", "2.2" },
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

    private static async Task<Message> Usage(ITelegramBotClient botClient, Message message,
        CancellationToken cancellationToken)
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

    private Pager GetDefaultChannelsPager(int currentPage)
    {
        return new Pager(currentPage, 5);
    }
}