using MediatR;
using Telegram.Bot;
using TelegramMonitorBot.Storage.Repositories.Abstractions;
using TelegramMonitorBot.TelegramBotClient.ChatContext;
using TelegramMonitorBot.TelegramBotClient.Extensions;
using TelegramMonitorBot.TelegramBotClient.Navigation;

namespace TelegramMonitorBot.TelegramBotClient.Application.Queries.EditChannelMenu;

public class EditChannelMenuRequestHandler : IRequestHandler<EditChannelMenuRequest>
{
    private readonly ITelegramBotClient _botClient;
    private readonly IChannelUserRepository _channelUserRepository;
    private readonly ChatContextManager _contextManager;

    public EditChannelMenuRequestHandler(
        ITelegramBotClient botClient,
        IChannelUserRepository channelUserRepository, 
        ChatContextManager contextManager)
    {
        _botClient = botClient;
        _channelUserRepository = channelUserRepository;
        _contextManager = contextManager;
    }

    public async Task Handle(EditChannelMenuRequest request, CancellationToken cancellationToken)
    {
        var channel = await _channelUserRepository.GetChannel(request.ChannelId, cancellationToken);
        var chatId = request.CallbackQuery.Message!.Chat.Id;
        var channelId = request.ChannelId;
        if (channel is null)
        {
            var channelNotFoundMessage = BotMessageBuilder.ChannelNotFound(chatId);
            await _botClient.SendTextMessageRequestAsync(channelNotFoundMessage, cancellationToken);

            return;
        }

        _contextManager.OnEditChannel(chatId, channelId);
        
        var channelSettingsMessage = BotMessageBuilder.GetChannelSettingsMessage(chatId, channel);
        await  _botClient.SendTextMessageRequestAsync(channelSettingsMessage, cancellationToken);
    }
}
