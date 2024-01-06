using MediatR;

namespace TelegramMonitorBot.TelegramBotClient.Application.Commands.AddPhrasesToChannel;

public record AddPhrasesToChannelRequest(long UserId, long ChannelId, IEnumerable<string> Phrases) : IRequest;
