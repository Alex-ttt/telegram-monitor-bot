using System.Text.Json;
using Amazon;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TelegramMonitorBot.AmazonSecretsManagerClient.Client;
using TelegramMonitorBot.Configuration.Options;

namespace TelegramMonitorBot.AmazonSecretsManagerClient;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSecretManagerClient(this IServiceCollection services, IConfiguration configuration)
    {
        var awsOptionsSection = configuration.GetSection("Aws");
        services.Configure<AwsOptions>(awsOptionsSection);

        var secretManager = AmazonSecretsManagerClientFactory.Create(configuration);
        services
            .AddSingleton<IAwsSecretManagerClient, AwsSecretManagerClient>()
            .AddSingleton(secretManager);

        return services;
    }

    public static IServiceCollection ConfigureAmazonSecrets(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .ConfigureTelegramApiOptions(configuration)
            .ConfigureTelegramBotApiOptions(configuration);
        
        return services;
    }

    private static IServiceCollection ConfigureTelegramApiOptions(this IServiceCollection services, IConfiguration configuration)
    {
        var telegramApiOptionsSection = configuration.GetSection("TelegramApiCredentials");
        var value = JsonSerializer.Deserialize<TelegramApiOptions>(telegramApiOptionsSection.Value!);
        services.Configure<TelegramApiOptions>(options => options.CopyFromAnother(value));

        return services;
    }
    
    private static IServiceCollection ConfigureTelegramBotApiOptions(this IServiceCollection services, IConfiguration configuration)
    {
        var telegramBotOptionsSection = configuration.GetSection("TelegramBotApiCredentials");
        var value = JsonSerializer.Deserialize<TelegramBotApiOptions>(telegramBotOptionsSection.Value!);
        
        services.Configure<TelegramBotApiOptions>(options => options.Token = value.Token);

        return services;
    }
}