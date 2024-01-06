using MediatR;

namespace TelegramMonitorBot.TelegramBotClient.Application.Commands.AddChannelUser;

public record AddChannelUserRequest(long UserId, string? UserName, string ChannelName) : IRequest;
