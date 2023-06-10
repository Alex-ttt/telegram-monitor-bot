using System.Text.Json;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Options;
using TelegramMonitorBot.AmazonSecretsManagerClient.Models;
using TelegramMonitorBot.Configuration.Options;

namespace TelegramMonitorBot.AmazonSecretsManagerClient.Client;

public class AwsSecretManagerClient : IAwsSecretManagerClient
{
    private readonly Amazon.SecretsManager.AmazonSecretsManagerClient _client;
    private readonly IOptions<AwsOptions> _options;

    public AwsSecretManagerClient(
        Amazon.SecretsManager.AmazonSecretsManagerClient client, 
        IOptions<AwsOptions> options)
    {
        _client = client;
        _options = options;
    }

    internal async Task<TelegramApiOptions> GetTelegramApiOptions(CancellationToken cancellationToken)
    {
        var request = new GetSecretValueRequest
        {
            SecretId = _options.Value.TelegramApiCredentialsName,
            VersionStage = "AWSCURRENT", // VersionStage defaults to AWSCURRENT if unspecified.
        };

        var response = await _client.GetSecretValueAsync(request, cancellationToken);

        var result = JsonSerializer.Deserialize<TelegramApiOptions>(response.SecretString)!;
        
        return result;
    }
    
    internal async Task<TelegramBotApiOptions> GetTelegramBotApiOptions(CancellationToken cancellationToken)
    {
        var request = new GetSecretValueRequest
        {
            SecretId = _options.Value.TelegramBotApiCredentialsName,
            VersionStage = "AWSCURRENT", // VersionStage defaults to AWSCURRENT if unspecified.
        };

        var response = await _client.GetSecretValueAsync(request, cancellationToken);
        var result = JsonSerializer.Deserialize<TelegramBotApiOptions>(response.SecretString)!;
        
        return result;
    }

    public async Task<VerificationCodeResponse> GetVerificationCode(CancellationToken cancellationToken)
    {
        var request = new GetSecretValueRequest
        {
            SecretId = _options.Value.TemporaryDataName,
            VersionStage = "AWSCURRENT", // VersionStage defaults to AWSCURRENT if unspecified.
        };

        var response = await _client.GetSecretValueAsync(request, cancellationToken);
        var result = JsonSerializer.Deserialize<VerificationCodeResponse>(response.SecretString)!;

        return result;
    }
}