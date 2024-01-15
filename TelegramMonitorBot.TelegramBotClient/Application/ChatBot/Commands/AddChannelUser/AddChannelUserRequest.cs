using MediatR;

namespace TelegramMonitorBot.TelegramBotClient.Application.ChatBot.Commands.AddChannelUser;

public record AddChannelUserRequest(long UserId, string? UserName, string ChannelName) : IRequest;
