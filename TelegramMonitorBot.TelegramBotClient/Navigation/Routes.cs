namespace TelegramMonitorBot.TelegramBotClient.Navigation;

public static class Routes
{
    public const string Menu = "/menu";
    public const string About = "/about";
    public const string Subscribe = "/subscribe";
    public const string MyChannels = "/my_channels";
    public const string PhraseIgnore = "/phrase_ignore";

    public static string MyChannelsPage(int page) => $"/my_channels_{page}";
    
    public static string EditChannel(long channelId) => $"/edit_channel_{channelId}";
    
    public static string ShowChannelPhrases(long channelId, int? page = null) => page is null ? $"/show_phrases_{channelId}" : $"/show_phrases_{channelId}_{page}";

    public static string AddPhrases(long channelId) => $"/add_phrases_to_{channelId}";
    
    public static string RemovePhrase(long channelId, string phrase) => $"/rm_phrase_{channelId}_{phrase}";
    
    public static string UnsubscribeFrom(long channelId) => $"/unsubscribe_from_{channelId}";
    public static string AcceptUnsubscribeFrom(long channelId) => $"/accept_unsubscribe_from_{channelId}";
}
