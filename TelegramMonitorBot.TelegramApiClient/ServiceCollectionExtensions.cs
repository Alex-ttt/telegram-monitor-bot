using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TelegramMonitorBot.TelegramApiClient;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTelegramApiClient(this IServiceCollection services)
    {
        services.AddSingleton<TdClientBuilder>();
        
        // This way allows TelegramApiClient class to be internal
        using var scope = services.BuildServiceProvider().CreateScope();
        var provider = scope.ServiceProvider;
        
        var logger = provider.GetRequiredService<ILogger<TelegramApiClient>>();
        var tdClientBuilder = provider.GetRequiredService<TdClientBuilder>();
        
        var tdClient = tdClientBuilder.GetLoggedInTdClient(CancellationToken.None).Result;
        services.AddSingleton(tdClient);
        services.AddScoped<ITelegramApiClient, TelegramApiClient>(t => new TelegramApiClient(tdClient, logger));
        return services;
    }
}
