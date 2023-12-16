using TelegramMonitorBot.DynamoDBMigrator;

namespace TelegramMonitorBot.Storage;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStorage(this IServiceCollection services)
    {
        services.AddSingleton<DynamoClientFactory>();

        var clientInitializer = services.BuildServiceProvider().GetRequiredService<DynamoClientFactory>();
        services.AddSingleton<StorageMigrator>(t => new StorageMigrator(clientInitializer.GetClient()));

        return services;
    }
    
}