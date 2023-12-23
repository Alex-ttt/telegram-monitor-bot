using Microsoft.Extensions.Configuration;

namespace TelegramMonitorBot.AmazonSecretsManagerClient;

internal class AmazonSecretsManagerConfigurationSource : IConfigurationSource
{
    private readonly Amazon.SecretsManager.AmazonSecretsManagerClient _secretClientManager;

    internal AmazonSecretsManagerConfigurationSource(Amazon.SecretsManager.AmazonSecretsManagerClient secretClientManager)
    {
        _secretClientManager = secretClientManager;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new AmazonSecretsManagerConfigurationProvider(_secretClientManager);
    }
}