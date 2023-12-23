using Amazon;
using Microsoft.Extensions.Configuration;

namespace TelegramMonitorBot.AmazonSecretsManagerClient;

internal static class AmazonSecretsManagerClientFactory
{
    internal static Amazon.SecretsManager.AmazonSecretsManagerClient Create(IConfiguration configuration)
    {
        var region = configuration.GetValue<string>("Aws:Region");
        var regionEndpoint = RegionEndpoint.GetBySystemName(region);
        var secretManager = new Amazon.SecretsManager.AmazonSecretsManagerClient(regionEndpoint);

        return secretManager;
    }
}