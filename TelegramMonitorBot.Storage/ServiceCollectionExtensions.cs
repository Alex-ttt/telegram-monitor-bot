using TelegramMonitorBot.DynamoDBMigrator;
using TelegramMonitorBot.Storage.Repositories;
using TelegramMonitorBot.Storage.Repositories.Abstractions;

namespace TelegramMonitorBot.Storage;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStorage(this IServiceCollection services)
    {
        services.AddSingleton<DynamoClientFactory>();

        var clientInitializer = services.BuildServiceProvider().GetRequiredService<DynamoClientFactory>();
        services.AddSingleton<StorageMigrator>(t => new StorageMigrator(clientInitializer.GetClient()));

        services.AddScoped<ITelegramRepository, TelegramRepository>();

        return services;
    }
    
}