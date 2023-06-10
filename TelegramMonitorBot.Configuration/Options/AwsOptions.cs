namespace TelegramMonitorBot.Configuration.Options;

public class AwsOptions
{
    public string Region { get; init; } = null!;
    public string TelegramApiCredentialsName { get; init; } = null!;
    public string TelegramBotApiCredentialsName { get; init; } = null!;
    public string TemporaryDataName { get; init; } = null!;
}
