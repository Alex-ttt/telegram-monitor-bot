namespace TelegramMonitorBot.TelegramApiClient.Models;

public record ChannelSearchMessages(long ChannelId, long LastMessage, Dictionary<string, ICollection<SearchMessage>> PhraseSearchMessages);
