using System.Text.Json.Serialization;

namespace TelegramMonitorBot.Configuration.Options;

public class TelegramApiOptions
{
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    [JsonPropertyName(Constants.JsonProperties.TelegramApi.ApiId)]
    public int ApiId { get; set; }

    [JsonPropertyName(Constants.JsonProperties.TelegramApi.ApiHash)]
    public string ApiHash { get; set; } = null!;
    
    [JsonPropertyName(Constants.JsonProperties.TelegramApi.SystemLanguageCode)]
    public string SystemLanguageCode { get; set; } = null!;
    
    [JsonPropertyName(Constants.JsonProperties.TelegramApi.ApplicationVersion)]
    public string ApplicationVersion { get; set; } = null!;
    
    [JsonPropertyName(Constants.JsonProperties.TelegramApi.DeviceModel)]
    public string DeviceModel { get; set; } = null!;
    
    [JsonPropertyName(Constants.JsonProperties.TelegramApi.PhoneNumber)]
    public string PhoneNumber { get; set; } = null!;

    internal void CopyFromAnother(TelegramApiOptions another)
    {
        ApiId = another.ApiId;
        ApiHash = another.ApiHash;
        SystemLanguageCode = another.SystemLanguageCode;
        ApplicationVersion = another.ApplicationVersion;
        DeviceModel = another.DeviceModel;
        PhoneNumber = another.PhoneNumber;
    }
}
