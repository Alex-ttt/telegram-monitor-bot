using MediatR;
using Telegram.Bot.Types;
using TelegramMonitorBot.TelegramBotClient.Application.Commands.AddChannelUser;
using TelegramMonitorBot.TelegramBotClient.Application.Commands.AddPhrasesToChannel;
using TelegramMonitorBot.TelegramBotClient.Application.Queries.About;
using TelegramMonitorBot.TelegramBotClient.Application.Queries.GetChannels;
using TelegramMonitorBot.TelegramBotClient.Application.Queries.GetMenu;
using TelegramMonitorBot.TelegramBotClient.Application.Queries.UnknownQuery;
using TelegramMonitorBot.TelegramBotClient.ChatContext;

namespace TelegramMonitorBot.TelegramBotClient.Routing;

public class MessageRouter
{
    private readonly ChatContextManager _contextManager;

    public MessageRouter(ChatContextManager contextManager)
    {
        _contextManager = contextManager;
    }

    public IBaseRequest RouteRequest(Message message)
    {
        var userId = message.Chat.Id;

        var result =
            TryRouteByTextCommand(userId, message.Text!)
            ?? TryRoteByContextState(message)
            ?? new UnknownQueryRequest(userId);
        
        return result;
    }
    
    private IBaseRequest? TryRouteByTextCommand(long userId, string text)
    {
        return text switch
        {
            "/menu" => new GetMenuRequest(userId),
            "/about" => new AboutRequest(userId),
            "/my_channels" => new GetChannelsRequest(userId, 1),
            _ => null
        };
    }
    
    private IBaseRequest? TryRoteByContextState(Message message)
    {
        var userId = message.Chat.Id;
        var context = _contextManager.GetCurrentContext(userId);
        
        return context switch
        {
            {CurrentState: ChatState.WaitingForChannelToSubscribe} =>
                new AddChannelUserRequest(userId, message.Chat.Username, message.Text!),

            {CurrentState: ChatState.WaitingForPhaseToAdd, ChannelId: { } channelIdToAddPhrase} =>
                new AddPhrasesToChannelRequest(userId, channelIdToAddPhrase, message.Text!.Split("\n")),

            _ => null
        };
    }
}