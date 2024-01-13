using TelegramMonitorBot.Domain.Models;

namespace TelegramMonitorBot.Storage.Repositories.Models;

public class UserChannelItemExtended
{
    public required long UserId { get; init; }
    public required Channel Channel { get; init; }
    public List<string>? Phrases { get; init; }
    public long? LastMessage { get; init; }
}