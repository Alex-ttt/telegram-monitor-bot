using TelegramMonitorBot.AmazonSecretsManagerClient.Models;

namespace TelegramMonitorBot.AmazonSecretsManagerClient.Client;

public interface IAwsSecretManagerClient
{
    Task<VerificationCodeResponse> GetVerificationCode(CancellationToken cancellationToken);
}
