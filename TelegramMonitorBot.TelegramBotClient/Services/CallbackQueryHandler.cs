using System.Text.RegularExpressions;
using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TelegramMonitorBot.TelegramBotClient.Services;

public class CallbackQueryHandler
{
    private const string ChannelPagePlaceholder = "page";
    private static readonly Regex ChannelPageRegex = new (@$"^\/my_channels(?<{ChannelPagePlaceholder}>_?\d+)?$", RegexOptions.Compiled);
    
    private const string EditChannelIdPlaceholder = "channelId";
    private static readonly Regex EditChannelIdRegex = new (@$"^\/edit_channel_(?<{EditChannelIdPlaceholder}>-?\d+)$", RegexOptions.Compiled);

    private const string AddPhrasesChannelIdPlaceholder = "channelId";
    private static readonly Regex AddPhrasesChannelIdRegex = new (@$"^\/add_phrases_to_(?<{AddPhrasesChannelIdPlaceholder}>-?\d+)$", RegexOptions.Compiled);
    
    private const string RemovePhrasesChannelIdPlaceholder = "channelId";
    private const string RemovePhrasesPagePlaceholder = "page";
    private static readonly Regex  RemovePhrasesChannelIdRegex = new (@$"^\/remove_phrases_from_(?<{RemovePhrasesChannelIdPlaceholder}>-?\d+)(?<{RemovePhrasesPagePlaceholder}>_\d+)?$", RegexOptions.Compiled);

    private const string RemovePrecisePhraseChannelIdPlaceholder = "channelId";
    private const string RemovePrecisePhrasePlaceholder = "phrase";
    private static readonly Regex RemovePrecisePhraseRegex =
        new (@$"^\/remove_phrase_(?<{RemovePrecisePhraseChannelIdPlaceholder}>-?\d+)_(?<{RemovePrecisePhrasePlaceholder}>.+)$", RegexOptions.Compiled);

    private const string UnsubscribeChannelIdPlaceholder = "channelId";
    private static readonly Regex UnsubscribeChannelIdRegex = new (@$"^\/unsubscribe_from_(?<{UnsubscribeChannelIdPlaceholder}>-?\d+)$", RegexOptions.Compiled);

    
    public static async Task Handle(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var callbackData = callbackQuery.Data;
        if (callbackData is null)
        {
            // TODO Do something
            return;    
        }
        
        var channelPageMatch = ChannelPageRegex.Match(callbackData);
        if (channelPageMatch.Success)
        {
            int? page = null;
            if (channelPageMatch.Groups[ChannelPagePlaceholder] is { Success: true} pageGroup)
            {
                page = (int.Parse(pageGroup.Value));
            }
            
            // await MyChannels(callbackQuery.Message!, pager, cancellationToken);
            return;
        }

        var editChannelMatch = EditChannelIdRegex.Match(callbackData);
        if (editChannelMatch.Success)
        {
            var channelIdString = editChannelMatch.Groups[EditChannelIdPlaceholder].Value;
            var channelId = long.Parse(channelIdString);
            // await EditChannel(callbackQuery.Message!, channelId, cancellationToken);
            return;
        }
        
        var addPhrasesMatch = AddPhrasesChannelIdRegex.Match(callbackData);
        if (addPhrasesMatch.Success)
        {
            var channelIdString = addPhrasesMatch.Groups[AddPhrasesChannelIdPlaceholder].Value;
            var channelId = long.Parse(channelIdString);
            // await PrepareForAddingPhrases(callbackQuery.Message!, channelId, cancellationToken);
            return;
        }
        
        var removePhrasesMatch = RemovePhrasesChannelIdRegex.Match(callbackData);
        if(removePhrasesMatch.Success)
        {
            var channelIdString = removePhrasesMatch.Groups[RemovePhrasesChannelIdPlaceholder].Value;
            var channelId = long.Parse(channelIdString);
            var removeChannelPage = 1;
            if (removePhrasesMatch.Groups[RemovePhrasesPagePlaceholder] is {Success: true} pageGroup)
            {
                removeChannelPage = int.Parse(pageGroup.Value);
            }
            
            // await ShowChannelPhrases(callbackQuery.Message!, channelId, page, cancellationToken);
            return;
        }

        var removePrecisePhraseMatch = RemovePrecisePhraseRegex.Match(callbackData);
        if (removePrecisePhraseMatch.Success)
        {
            var channelIdString = removePrecisePhraseMatch.Groups[RemovePrecisePhraseChannelIdPlaceholder].Value;
            var channelId = long.Parse(channelIdString);
            var phrase = removePrecisePhraseMatch.Groups[RemovePrecisePhrasePlaceholder].Value;
            // await RemovePrecisePhrase(callbackQuery, channelId, phrase, cancellationToken);
            return;
        }
        
        var unsubscribeFromChannelMatch = UnsubscribeChannelIdRegex.Match(callbackData);
        if (unsubscribeFromChannelMatch.Success)
        {
            var channelIdString = unsubscribeFromChannelMatch.Groups[UnsubscribeChannelIdPlaceholder].Value;
            var channelId = long.Parse(channelIdString);
            // await Unsubscribe(callbackQuery, channelId, cancellationToken);
            return;
        }
        
        if (callbackQuery.Data == "/subscribe")
        {
            // await Subscribe(callbackQuery.Message!, cancellationToken);
            return;
        }
    }
}