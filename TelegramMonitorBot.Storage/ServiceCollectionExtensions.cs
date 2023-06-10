using Microsoft.Extensions.Options;
using TelegramMonitorBot.Configuration.Options;

namespace TelegramMonitorBot.Storage;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStorage(this IServiceCollection services)
    {
        var awsOptions = services.BuildServiceProvider().GetRequiredService<IOptions<AwsOptions>>();
        services.AddSingleton<DynamoClientInitializer>(new DynamoClientInitializer(awsOptions));
        services.AddScoped<RepositoryBase>();

        // using var scope = services.BuildServiceProvider().CreateScope();
        // var repo = scope.ServiceProvider.GetRequiredService<RepositoryBase>();
        //
        // repo.DoSomething(CancellationToken.None).GetAwaiter().GetResult();

        return services;
    }
    
    
}