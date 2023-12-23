namespace TelegramMonitorBot.Storage.Exceptions;

public class UserChannelAlreadyExistsException : Exception
{
    public UserChannelAlreadyExistsException(long userId, long channelId, Exception? innerException = null) 
        : base($"User {userId} already connected with channel {channelId}", innerException)
    {
    }
}