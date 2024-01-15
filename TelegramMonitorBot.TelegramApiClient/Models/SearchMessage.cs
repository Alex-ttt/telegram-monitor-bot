namespace TelegramMonitorBot.TelegramApiClient.Models;

// TODO https://stackoverflow.com/questions/40561484/what-data-type-should-be-used-for-timestamp-in-dynamodb
public record SearchMessage(long Id, string Link, DateTimeOffset Date);
