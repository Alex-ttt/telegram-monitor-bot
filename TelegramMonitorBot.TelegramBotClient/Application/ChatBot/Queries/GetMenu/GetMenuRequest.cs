using MediatR;

namespace TelegramMonitorBot.TelegramBotClient.Application.ChatBot.Queries.GetMenu;

public record GetMenuRequest(long ChatId) : IRequest;
