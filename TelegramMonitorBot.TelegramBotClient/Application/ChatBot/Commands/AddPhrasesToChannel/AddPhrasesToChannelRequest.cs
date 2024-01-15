using MediatR;

namespace TelegramMonitorBot.TelegramBotClient.Application.ChatBot.Commands.AddPhrasesToChannel;

public record AddPhrasesToChannelRequest(long UserId, long ChannelId, IEnumerable<string> Phrases) : IRequest;
