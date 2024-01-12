namespace TelegramMonitorBot.TelegramBotClient.ChatContext;

/// <summary>
/// Represents various states in the chat with a precise user
/// </summary>
public enum ChatState
{
    MainMenu,
    ChannelsList,
    About,
    WaitingForChannelToSubscribe,
    Subscribed,
    AskUnsubscribe,
    EditChannel,
    WaitingForPhaseToAdd,
    PhrasesAdded,
    RetrieveChannelPhrases
}