namespace TelegramMonitorBot.Domain.Models;

public record SearchResult(string Phrase, List<Message> Messages);
