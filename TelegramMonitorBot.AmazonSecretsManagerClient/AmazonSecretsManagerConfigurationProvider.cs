using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Configuration;

namespace TelegramMonitorBot.AmazonSecretsManagerClient;

public class AmazonSecretsManagerConfigurationProvider(IAmazonSecretsManager secretClientManager)
    : ConfigurationProvider
{
    private static readonly HashSet<string> NotMappedConfig = new() {"TemporaryData"};

    public override void Load()
    {
        var secret = GetSecrets().Result;
        Data = secret;
    }

    private async Task<IDictionary<string, string?>> GetSecrets()
    {
        var listSecretsRequest = new ListSecretsRequest();
        var listSecretsResponse = await secretClientManager.ListSecretsAsync(listSecretsRequest);
        var result = new Dictionary<string, string>(listSecretsResponse.SecretList.Count);

        foreach (var secretEntry in listSecretsResponse.SecretList.Where(t => NotMappedConfig.Contains(t.Name) is false))
        {
            var secret = await secretClientManager.GetSecretValueAsync(new GetSecretValueRequest {SecretId = secretEntry.ARN});
            var secretString = ReadSecret(secret);
            
            result.Add(secretEntry.Name, secretString);
        }

        return result;
    }

    private static string ReadSecret(GetSecretValueResponse secret)
    {
        if (secret.SecretString is not null)
        {
            return secret.SecretString;
        }
        
        var memoryStream = secret.SecretBinary;
        var reader = new StreamReader(memoryStream);
        var secretString = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(reader.ReadToEnd()));
        
        return secretString;
    }
}