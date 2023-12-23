using Microsoft.Extensions.Configuration;

namespace TelegramMonitorBot.AmazonSecretsManagerClient;

public static class HostBuilderExtensions
{
    public static void AddAmazonSecretsManager(this IConfigurationBuilder configurationBuilder, IConfiguration configuration)
    {

        var secretManager = AmazonSecretsManagerClientFactory.Create(configuration);
        var configurationSource = new AmazonSecretsManagerConfigurationSource(secretManager);

        configurationBuilder.Add(configurationSource);
    }
}