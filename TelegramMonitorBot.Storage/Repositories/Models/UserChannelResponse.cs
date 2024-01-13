namespace TelegramMonitorBot.Storage.Repositories.Models;

public record UserChannelResponse(ICollection<UserChannelItemExtended> Items)
{
    public static readonly UserChannelResponse Empty = new(Array.Empty<UserChannelItemExtended>());
}

