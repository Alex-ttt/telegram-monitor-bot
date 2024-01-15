using MediatR;

namespace TelegramMonitorBot.TelegramBotClient.Application.ChatBot.Queries.GetChannels;

public record GetChannelsRequest(long ChatId, int? Page) : IRequest;
