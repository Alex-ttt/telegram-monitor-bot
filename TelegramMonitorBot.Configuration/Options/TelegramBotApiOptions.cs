using System.Text.Json.Serialization;

namespace TelegramMonitorBot.Configuration.Options;

public class TelegramBotApiOptions
{
    [JsonPropertyName(Constants.JsonProperties.TelegramBotApi.Token)]
    public string Token { get; set; } = null!;

    internal void CopyFromAnother(TelegramBotApiOptions another) => Token = another.Token;
}
