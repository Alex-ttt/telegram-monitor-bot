namespace TelegramMonitorBot.TelegramBotClient.ChatContext;

public class ChatContextManager
{
    private readonly Dictionary<long, ChatContext> _userContexts = new ();

    public ChatState GetCurrentState(long chatId)
    {
        var userContext = GetChatContext(chatId);
        return userContext.CurrentState;
    }
    
    public void OnAddingChannel(long chatId)
    {
        var userContext = GetChatContext(chatId);
        userContext.Move(ChatState.WaitingForChannelToSubscribe, UserAction.CallSubscribeChannel);
    }
    
    public void OnAddedChannel(long chatId)
    {
        var userContext = GetChatContext(chatId);
        userContext.Move(ChatState.Subscribed, UserAction.SubscribeToChannel);
    }
    

    private ChatContext GetChatContext(long chatId)
    {
        if (_userContexts.TryGetValue(chatId, out var userContext))
        {
            return userContext;
        }
        
        _userContexts.TryAdd(chatId, new ChatContext(chatId));
        userContext = _userContexts[chatId];

        return userContext;
    }
    
}

public class ChatContext
{

    private readonly List<UserAction> _userActions = new();

    public ChatContext(long chatId)
    {
        ChatId = chatId;
    }
    
    public long ChatId { get; }
    
    public IReadOnlyCollection<UserAction> UserActions => _userActions;
    
    public ChatState CurrentState { get; private set; } = ChatState.MainMenu;


    public void Move(ChatState state, UserAction action)
    {
        CurrentState = state;
        _userActions.Add(action);
    }
}