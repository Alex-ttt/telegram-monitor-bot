﻿using System.Text.RegularExpressions;
using MediatR;
using Telegram.Bot.Types;
using TelegramMonitorBot.TelegramBotClient.Application.ChatBot.Commands.AcceptUnsubscribeFromChannel;
using TelegramMonitorBot.TelegramBotClient.Application.ChatBot.Commands.PrepareChannelForPhrasesAdding;
using TelegramMonitorBot.TelegramBotClient.Application.ChatBot.Commands.RemovePhrase;
using TelegramMonitorBot.TelegramBotClient.Application.ChatBot.Commands.TurnOnSubscribeMode;
using TelegramMonitorBot.TelegramBotClient.Application.ChatBot.Common;
using TelegramMonitorBot.TelegramBotClient.Application.ChatBot.Queries.EditChannelMenu;
using TelegramMonitorBot.TelegramBotClient.Application.ChatBot.Queries.GetChannelPhrases;
using TelegramMonitorBot.TelegramBotClient.Application.ChatBot.Queries.GetChannels;
using TelegramMonitorBot.TelegramBotClient.Application.ChatBot.Queries.UnsubscribeFromChannel;

namespace TelegramMonitorBot.TelegramBotClient.Navigation.Routing;

public class CallbackQueryRouter
{
    private const string ChannelPagePlaceholder = "page";
    private static readonly Regex ChannelPageRegex = new (@$"^\/my_channels(?<{ChannelPagePlaceholder}>_?\d+)?$", RegexOptions.Compiled);
    
    private const string EditChannelIdPlaceholder = "channelId";
    private static readonly Regex EditChannelIdRegex = new (@$"^\/edit_channel_(?<{EditChannelIdPlaceholder}>-?\d+)$", RegexOptions.Compiled);

    private const string AddPhrasesChannelIdPlaceholder = "channelId";
    private static readonly Regex AddPhrasesChannelIdRegex = new (@$"^\/add_phrases_to_(?<{AddPhrasesChannelIdPlaceholder}>-?\d+)$", RegexOptions.Compiled);
    
    private const string ShowPhrasesChannelIdPlaceholder = "channelId";
    private const string ShowPhrasesPagePlaceholder = "page";
    private static readonly Regex ShowPhrasesChannelIdRegex = new (@$"^\/show_phrases_(?<{ShowPhrasesChannelIdPlaceholder}>-?\d+)(?<{ShowPhrasesPagePlaceholder}>_\d+)?$", RegexOptions.Compiled);

    private const string RemovePrecisePhraseChannelIdPlaceholder = "channelId";
    private const string RemovePrecisePhrasePlaceholder = "phrase";
    private static readonly Regex RemovePrecisePhraseRegex =
        new (@$"^\/rm_phrase_(?<{RemovePrecisePhraseChannelIdPlaceholder}>-?\d+)_(?<{RemovePrecisePhrasePlaceholder}>.+)$", RegexOptions.Compiled);

    private const string UnsubscribeChannelIdPlaceholder = "channelId";
    private static readonly Regex UnsubscribeChannelIdRegex = new (@$"^\/unsubscribe_from_(?<{UnsubscribeChannelIdPlaceholder}>-?\d+)$", RegexOptions.Compiled);

    private const string AcceptUnsubscribeChannelIdPlaceholder = "channelId";
    private static readonly Regex AcceptUnsubscribeChannelIdRegex = new (@$"^\/accept_unsubscribe_from_(?<{AcceptUnsubscribeChannelIdPlaceholder}>-?\d+)$", RegexOptions.Compiled);
    
    public IBaseRequest? RouteRequest(CallbackQuery callbackQuery)
    {
        var callbackData = callbackQuery.Data;
        if (callbackData is null)
        {
            // TODO Do something
            return null;
        }

        if (TryRouteChannelsPage(callbackQuery) is { } channelPageRequest)
        {
            return channelPageRequest;
        }

        if (TryRouteEditChannelPage(callbackQuery) is { } editChannelPageRequest)
        {
            return editChannelPageRequest;
        }

        if (TryRoutePrepareChannelForPhraseAdding(callbackQuery) is { } prepareChannelForPhraseAddingRequest)
        {
            return prepareChannelForPhraseAddingRequest;
        }

        if (TryRouteRemovePhrasesFromChannel(callbackQuery) is { } removePhrasesFromChannelRequest)
        {
            return removePhrasesFromChannelRequest;
        }

        if (TryRouteRemovePrecisePhrase(callbackQuery) is { } removePhraseRequest)
        {
            return removePhraseRequest;
        }

        if (TryRouteUnsubscribe(callbackQuery) is { } unsubscribeRequest)
        {
            return  unsubscribeRequest;
        }
        
        if (TryRouteSubscribe(callbackQuery) is { } subscribeRequest)
        {
            return subscribeRequest;
        }

        if (TryRouteAcceptUnsubscribe(callbackQuery) is { } acceptUnsubscribeRequest)
        {
            return acceptUnsubscribeRequest;
        }

        if (TryRouteIgnoreCallback(callbackQuery) is { } ignoreCallbackRequest)
        {
            return  ignoreCallbackRequest;
        }
        
        return null;
    }

    private AcceptUnsubscribeFromChannelRequest? TryRouteAcceptUnsubscribe(CallbackQuery callbackQuery)
    {
        var unsubscribeFromChannelMatch = AcceptUnsubscribeChannelIdRegex.Match(callbackQuery.Data!);
        if (unsubscribeFromChannelMatch.Success)
        {
            var channelIdString = unsubscribeFromChannelMatch.Groups[AcceptUnsubscribeChannelIdPlaceholder].Value;
            var channelId = long.Parse(channelIdString);
            
            return new AcceptUnsubscribeFromChannelRequest(callbackQuery, channelId);
        }

        return null;
    }

    private static IgnoreQueryRequest? TryRouteIgnoreCallback(CallbackQuery callbackQuery)
    {
        return callbackQuery.Data is Routes.PhraseIgnore ? IgnoreQueryRequest.Instance : null;
    }


    private static TurnOnSubscribeModeRequest? TryRouteSubscribe(CallbackQuery callbackQuery)
    {
        return callbackQuery.Data is Routes.Subscribe ? new TurnOnSubscribeModeRequest(callbackQuery) : null;
    }
    
    private static AskUnsubscribeFromChannelRequest? TryRouteUnsubscribe(CallbackQuery callbackQuery)
    {
        var unsubscribeFromChannelMatch = UnsubscribeChannelIdRegex.Match(callbackQuery.Data!);
        if (unsubscribeFromChannelMatch.Success)
        {
            var channelIdString = unsubscribeFromChannelMatch.Groups[UnsubscribeChannelIdPlaceholder].Value;
            var channelId = long.Parse(channelIdString);
            
            return new AskUnsubscribeFromChannelRequest(callbackQuery, channelId);
        }

        return null;
    }
    
    private static RemovePhraseRequest? TryRouteRemovePrecisePhrase(CallbackQuery callbackQuery)
    {
        var removePrecisePhraseMatch = RemovePrecisePhraseRegex.Match(callbackQuery.Data!);
        if (removePrecisePhraseMatch.Success)
        {
            var channelIdString = removePrecisePhraseMatch.Groups[RemovePrecisePhraseChannelIdPlaceholder].Value;
            var channelId = long.Parse(channelIdString);
            var phrase = removePrecisePhraseMatch.Groups[RemovePrecisePhrasePlaceholder].Value;
            
            return new RemovePhraseRequest(callbackQuery, channelId, phrase);
        }

        return null;
    }

    private static EditChannelMenuRequest? TryRouteEditChannelPage(CallbackQuery callbackQuery)
    {
        var editChannelMatch = EditChannelIdRegex.Match(callbackQuery.Data!);
        if (editChannelMatch.Success)
        {
            var channelIdString = editChannelMatch.Groups[EditChannelIdPlaceholder].Value;
            var channelId = long.Parse(channelIdString);
            var editChannelMenuRequest = new EditChannelMenuRequest(callbackQuery, channelId);

            return editChannelMenuRequest;
        }

        return null;
    }

    private static GetChannelsRequest? TryRouteChannelsPage(CallbackQuery callbackQuery)
    {
        var channelPageMatch = ChannelPageRegex.Match(callbackQuery.Data!);
        if (channelPageMatch.Success)
        {
            int? page = null;
            if (channelPageMatch.Groups[ChannelPagePlaceholder] is {Success: true} pageGroup)
            {
                page = int.Parse(pageGroup.Value);
            }

            var myChannelsRequest = new GetChannelsRequest(callbackQuery.Message!.Chat.Id, page);
            return myChannelsRequest;
        }

        return null;
    }

    private static PrepareChannelForPhrasesAddingRequest? TryRoutePrepareChannelForPhraseAdding(CallbackQuery callbackQuery)
    {
        var addPhrasesMatch = AddPhrasesChannelIdRegex.Match(callbackQuery.Data!);
        if (addPhrasesMatch.Success)
        {
            var channelIdString = addPhrasesMatch.Groups[AddPhrasesChannelIdPlaceholder].Value;
            var channelId = long.Parse(channelIdString);

            return new PrepareChannelForPhrasesAddingRequest(callbackQuery, channelId);
        }
        
        return null;
    }

    private static GetChannelPhrasesRequest? TryRouteRemovePhrasesFromChannel(CallbackQuery callbackQuery)
    {
        var removePhrasesMatch = ShowPhrasesChannelIdRegex.Match(callbackQuery.Data!);
        if(removePhrasesMatch.Success)
        {
            var channelIdString = removePhrasesMatch.Groups[ShowPhrasesChannelIdPlaceholder].Value;
            var channelId = long.Parse(channelIdString);
            var removeChannelPage = 1;
            if (removePhrasesMatch.Groups[ShowPhrasesPagePlaceholder] is {Success: true} pageGroup)
            {
                removeChannelPage = int.Parse(pageGroup.Value);
            }
            
            return new GetChannelPhrasesRequest(callbackQuery, channelId, removeChannelPage);
        }

        return null;
    }
}