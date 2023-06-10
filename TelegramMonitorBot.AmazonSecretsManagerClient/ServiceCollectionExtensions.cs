using Amazon;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TelegramMonitorBot.AmazonSecretsManagerClient.Client;
using TelegramMonitorBot.Configuration.Options;

namespace TelegramMonitorBot.AmazonSecretsManagerClient;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSecretManagerClient(this IServiceCollection services, IConfiguration configuration)
    {
        var awsOptionsSection = configuration.GetSection("Aws");
        services.Configure<AwsOptions>(awsOptionsSection);
        
        var region = awsOptionsSection.GetValue<string>("Region");
        var regionEndpoint = RegionEndpoint.GetBySystemName(region);

        services
            .AddSingleton<Amazon.SecretsManager.AmazonSecretsManagerClient>(_ => new Amazon.SecretsManager.AmazonSecretsManagerClient(regionEndpoint))
            .AddSingleton<IAwsSecretManagerClient, AwsSecretManagerClient>();
            
        using var scope = services.BuildServiceProvider().CreateScope();
        
        services
            .ConfigureTelegramApiOptions(scope)
            .ConfigureTelegramBotApiOptions(scope);

        return services;
    }

    private static IServiceCollection ConfigureTelegramApiOptions(this IServiceCollection services, IServiceScope scope)
    {
        var client = GetAwsClientManager(services, scope);
        var telegramOptions = client.GetTelegramApiOptions(CancellationToken.None).Result;
        
        services.Configure<TelegramApiOptions>(options => options.CopyFromAnother(telegramOptions));

        return services;
    }
    
    
    private static IServiceCollection ConfigureTelegramBotApiOptions(this IServiceCollection services, IServiceScope scope)
    {
        var client = GetAwsClientManager(services, scope);
        var telegramBotApiOptions = client.GetTelegramBotApiOptions(CancellationToken.None).Result;
        
        services.Configure<TelegramBotApiOptions>(options => options.CopyFromAnother(telegramBotApiOptions));

        return services;
    }

    private static AwsSecretManagerClient GetAwsClientManager(IServiceCollection services, IServiceScope scope)
    {
        var serviceProvider = scope.ServiceProvider;
        if (serviceProvider.GetRequiredService<IAwsSecretManagerClient>() is not AwsSecretManagerClient client)
        {
            throw new InvalidOperationException($"Unexpected implementation type of {nameof(IAwsSecretManagerClient)}");
        }

        return client;
    }
    
}