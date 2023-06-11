using TelegramMonitorBot.DynamoDBMigrator;

namespace TelegramMonitorBot.Storage;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStorage(this IServiceCollection services)
    {
        services.AddSingleton<DynamoClientInitializer>();

        var clientInitializer = services.BuildServiceProvider().GetRequiredService<DynamoClientInitializer>();
        services.AddSingleton<StorageMigrator>(t => new StorageMigrator(clientInitializer.GetClient()));

        return services;
    }
    
    
}