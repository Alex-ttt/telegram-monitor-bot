namespace TelegramMonitorBot.TelegramBotClient.ChatContext;

public class ChatContextManager
{
    private readonly Dictionary<long, ChatContext> _userContexts = new();

    public ChatContext GetCurrentContext(long chatId)
    {
        var userContext = GetChatContext(chatId);
        return userContext;
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
    
    public void OnPhrasesAdding(long chatId, long channelId)
    {
        var userContext = GetChatContext(chatId);
        userContext.Move(ChatState.WaitingForPhaseToAdd, UserAction.CallAddPhrases, channelId);
    }
    
    public void OnPhrasesAdded(long chatId, long channelId)
    {
        var userContext = GetChatContext(chatId);
        userContext.Move(ChatState.PhrasesAdded, UserAction.PhrasesAdded, channelId);
    }
    
    public void OnMainMenu(long chatId)
    {
        var userContext = GetChatContext(chatId);
        userContext.Move(ChatState.MainMenu, UserAction.MainMenu);
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

    public void OnChannelRemoved(long chatId)
    {
        var userContext = GetChatContext(chatId);
        userContext.Move(ChatState.ChannelsList, UserAction.UnsubscribeFromChannel);
    }

    public void OnPhraseRemoved(long chatId)
    {
        var userContext = GetChatContext(chatId);
        userContext.Move(ChatState.ChannelsList, UserAction.PhraseRemoved);
    }

    public void OnAbout(long chatId)
    {
        var userContext = GetChatContext(chatId);
        userContext.Move(ChatState.About, UserAction.About);
    }

    public void OnEditChannel(long chatId, long channelId)
    {
        var userContext = GetChatContext(chatId);
        userContext.Move(ChatState.EditChannel, UserAction.EditChannel, channelId);
    }

    public void OnGetChannelPhrases(long chatId)
    {
        var userContext = GetChatContext(chatId);
        userContext.Move(ChatState.RetrieveChannelPhrases, UserAction.RetrieveChannelPhrases);
    }

    public void OnChannelsList(long chatId)
    {
        var userContext = GetChatContext(chatId);
        userContext.Move(ChatState.ChannelsList, UserAction.ChannelsList);
    }

    public void OnAskUnsubscribe(long chatId, long channelId)
    {
        var userContext = GetChatContext(chatId);
        userContext.Move(ChatState.AskUnsubscribe, UserAction.AskUnsubscribeFromChannel, channelId);
        
    }
}

public class ChatContext
{

    private readonly List<UserAction> _userActions = new();

    public long? ChannelId;

    public ChatContext(long chatId)
    {
        ChatId = chatId;
    }
    
    public long ChatId { get; }
    
    public IReadOnlyCollection<UserAction> UserActions => _userActions;
    
    public ChatState CurrentState { get; private set; } = ChatState.MainMenu;
    
    public void Move(ChatState state, UserAction action, long? channelId = null)
    {
        CurrentState = state;
        ChannelId = channelId;
        _userActions.Add(action);
    }
}