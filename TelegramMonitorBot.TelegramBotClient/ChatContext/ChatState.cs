namespace TelegramMonitorBot.TelegramBotClient.ChatContext;

public enum ChatState
{
    MainMenu,
    WaitingForChannelToSubscribe,
    WaitingForChannelToUnsubscribe,
    WaitingForChannelToUnsubscribeConfirmation, // Think about using it later
    Subscribed,
    Unsubscribed,
    WaitingForPhaseToAdd,
    WaitingForPhaseToRemove,
}