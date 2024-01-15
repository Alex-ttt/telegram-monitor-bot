using Microsoft.Extensions.DependencyInjection;

namespace TelegramMonitorBot.TelegramApiClient;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTelegramApiClient(this IServiceCollection services)
    {
        services.AddSingleton<TdClientBuilder>();
        
        // This way allows TelegramApiClient class to be internal
        using var scope = services.BuildServiceProvider().CreateScope();
        var provider = scope.ServiceProvider;
        
        var tdClientBuilder = provider.GetRequiredService<TdClientBuilder>();
        
        var tdClient = tdClientBuilder.GetLoggedInTdClient(CancellationToken.None).Result;
        services.AddSingleton(tdClient);
        services.AddScoped<ITelegramApiClient, TelegramApiClient>(_ => new TelegramApiClient(tdClient));
        return services;
    }
}
