namespace TelegramMonitorBot.Storage.Exceptions;

public class ConcurrentUpdateException : Exception
{
    public ConcurrentUpdateException(Exception? innerException = null) : base("Error due concurrent update of one item by several clients", innerException)
    {
        
    }
}
