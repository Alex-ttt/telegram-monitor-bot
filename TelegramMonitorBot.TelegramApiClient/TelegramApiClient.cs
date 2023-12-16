using Microsoft.Extensions.Logging;
using TdLib;

namespace TelegramMonitorBot.TelegramApiClient;

internal class TelegramApiClient : ITelegramApiClient
{
    private readonly TdClient _tdClient;
    private readonly ILogger _logger;

    internal TelegramApiClient(TdClient tdClient, ILogger<TelegramApiClient> logger) =>
        (_tdClient, _logger) = (tdClient, logger);

    public async Task DoStuff()
    {
        var user1 = await _tdClient.SearchUserByPhoneNumberAsync("****");
        var user2 = await _tdClient.GetUserAsync(77777);
        var chat = await _tdClient.SearchPublicChatAsync("****");

        
        _logger.LogInformation("It works!");
    }
}