namespace TelegramMonitorBot.TelegramBotClient.ChatContext;

/// <summary>
/// Represents user actions 
/// </summary>
public enum UserAction
{
    MainMenu,
    ChannelsList,
    About,
    CallSubscribeChannel,
    SubscribeToChannel,
    AskUnsubscribeFromChannel,
    UnsubscribeFromChannel,
    EditChannel,
    CallAddPhrases,
    PhrasesAdded,
    PhraseRemoved,
    RetrieveChannelPhrases
}
