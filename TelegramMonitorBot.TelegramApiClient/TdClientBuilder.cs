using Microsoft.Extensions.Options;
using TdLib;
using TelegramMonitorBot.AmazonSecretsManagerClient.Client;
using TelegramMonitorBot.AmazonSecretsManagerClient.Models;
using TelegramMonitorBot.Configuration.Options;


namespace TelegramMonitorBot.TelegramApiClient;

internal class TdClientBuilder
{
    private readonly IAwsSecretManagerClient _awsSecretManagerClient;
    private readonly IOptions<TelegramApiOptions> _options;

    public TdClientBuilder(IAwsSecretManagerClient awsSecretManagerClient, IOptions<TelegramApiOptions> options)
    {
        _awsSecretManagerClient = awsSecretManagerClient;
        _options = options;
    }

    internal Task LogInTdClient(TdClient client, CancellationToken cancellationToken) => LogIn(client, cancellationToken);

    internal async Task<TdClient> GetLoggedInTdClient(CancellationToken cancellationToken)
    {
        var client = new TdClient();
        await LogIn(client, cancellationToken);

        return client;
    }

    private async Task LogIn(TdClient client, CancellationToken cancellationToken)
    {
        var apiOptions = _options.Value;
        
        await client.SetTdlibParametersAsync(
            apiId: apiOptions.ApiId, 
            apiHash: apiOptions.ApiHash, 
            systemLanguageCode: apiOptions.SystemLanguageCode,
            applicationVersion: apiOptions.ApplicationVersion,
            deviceModel: apiOptions.DeviceModel);

        await client.SetLogVerbosityLevelAsync(1);
        
        var currentState = await client.GetAuthorizationStateAsync();
        if (currentState is TdApi.AuthorizationState.AuthorizationStateReady)
        {
            return;
        }


        _ = await client.SetAuthenticationPhoneNumberAsync(apiOptions.PhoneNumber);

        var code = await AwaitVerificationCode(cancellationToken);
        
        TdApi.Object authResult = await client.CheckAuthenticationCodeAsync(code);

        // Wait for authorization to complete
        while (authResult is not TdApi.AuthorizationState.AuthorizationStateReady)
        {
            authResult = await client.ExecuteAsync(new TdApi.GetAuthorizationState());
        }
        
        Console.WriteLine("Authorization complete!");
    }
    

    private async Task<string> AwaitVerificationCode(CancellationToken cancellationToken)
    {
        
#if DEBUG
        return CodeFromConsole();
#else
        return await CodeFromSecrets(cancellationToken);
#endif
    }

    private async Task<string> CodeFromSecrets(CancellationToken cancellationToken)
    {
        var delay = TimeSpan.FromSeconds(30);
        VerificationCodeResponse response;
        do
        {
            await Task.Delay(delay, cancellationToken);
            response = await _awsSecretManagerClient.GetVerificationCode(cancellationToken);
        } while (string.IsNullOrEmpty(response.VerificationCode));
        
        return response.VerificationCode;
    }

    private static string CodeFromConsole()
    {
        Console.WriteLine("Verification code: ");
        return Console.ReadLine() ?? string.Empty;
    }
    
    
}
