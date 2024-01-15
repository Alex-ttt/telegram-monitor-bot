using MediatR;

namespace TelegramMonitorBot.TelegramBotClient.Application.ChatBot.Queries.About;

public record AboutRequest(long ChatId) : IRequest;
