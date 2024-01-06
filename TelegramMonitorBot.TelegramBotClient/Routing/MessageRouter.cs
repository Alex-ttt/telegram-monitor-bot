using MediatR;
using Telegram.Bot.Types;
using TelegramMonitorBot.TelegramBotClient.Application.Commands.AddChannelUser;
using TelegramMonitorBot.TelegramBotClient.Application.Commands.AddPhrasesToChannel;
using TelegramMonitorBot.TelegramBotClient.ChatContext;

namespace TelegramMonitorBot.TelegramBotClient.Routing;

public class MessageRouter
{
    private readonly ChatContextManager _contextManager;

    public MessageRouter(ChatContextManager contextManager)
    {
        _contextManager = contextManager;
    }

    public IBaseRequest? RouteRequest(Message message)
    {
        var context = _contextManager.GetCurrentContext(message.Chat.Id);
        var userId = message.Chat.Id;

        IBaseRequest? result = context switch
        {
            {CurrentState: ChatState.WaitingForChannelToSubscribe} =>
                new AddChannelUserRequest(userId, message.Chat.Username, message.Text!),

            {CurrentState: ChatState.WaitingForPhaseToAdd, ChannelId: { } channelIdToAddPhrase} =>
                new AddPhrasesToChannelRequest(userId, channelIdToAddPhrase, message.Text!.Split("\n")),

            _ => null
        };

        return result;
    }
}