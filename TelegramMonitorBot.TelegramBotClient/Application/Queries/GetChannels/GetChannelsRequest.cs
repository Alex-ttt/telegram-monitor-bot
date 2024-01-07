using MediatR;

namespace TelegramMonitorBot.TelegramBotClient.Application.Queries.GetChannels;

public record GetChannelsRequest(long ChatId, int? Page) : IRequest;
