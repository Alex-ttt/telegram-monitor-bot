namespace TelegramMonitorBot.Domain.Models;

public record Message(long Id, string Link, DateTimeOffset Date);
