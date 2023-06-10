namespace TelegramMonitorBot.Configuration;

internal static class Constants
{
    internal static class JsonProperties
    {
        internal static class TelegramApi
        {
            internal const string ApiId = "TDLib_apiId";
            internal const string ApiHash = "TDLib_apiHash";
            internal const string SystemLanguageCode = "TDLib_systemLanguageCode";
            internal const string ApplicationVersion = "TDLib_applicationVersion";
            internal const string DeviceModel = "TDLib_deviceModel";
            internal const string PhoneNumber = "TDLib_phoneNumber";
        }

        internal static class TelegramBotApi
        {
            internal const string Token = "BotClient_token";
        }
    }
}
