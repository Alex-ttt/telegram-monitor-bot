using MediatR;

namespace TelegramMonitorBot.TelegramBotClient.Application.Queries.GetMenu;

public record GetMenuRequest(long ChatId) : IRequest;
