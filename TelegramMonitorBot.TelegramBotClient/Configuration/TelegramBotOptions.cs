namespace TelegramMonitorBot.TelegramBotClient.Configuration;

public class TelegramBotOptions
{
    internal string Token { get; private set; } = string.Empty;

    public TelegramBotOptions SetToken(string value)
    {
        Token = value;
        return this;
    }
}