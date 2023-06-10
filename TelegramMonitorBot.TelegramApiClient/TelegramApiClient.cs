using Microsoft.Extensions.Logging;
using TdLib;

namespace TelegramMonitorBot.TelegramApiClient;

internal class TelegramApiClient : ITelegramApiClient
{
    private readonly TdClient _tdClient;
    private readonly ILogger _logger;

    internal TelegramApiClient(TdClient tdClient, ILogger<TelegramApiClient> logger) =>
        (_tdClient, _logger) = (tdClient, logger);

    public void DoStuff()
    {
        _logger.LogInformation("It works!");
    }
}