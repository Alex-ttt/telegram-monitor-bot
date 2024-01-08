using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using TelegramMonitorBot.Configuration.Options;
using TelegramMonitorBot.TelegramBotClient.ChatContext;
using TelegramMonitorBot.TelegramBotClient.Navigation;
using TelegramMonitorBot.TelegramBotClient.Navigation.Routing;
using TelegramMonitorBot.TelegramBotClient.Services;

namespace TelegramMonitorBot.TelegramBotClient;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTelegramBotClient(this IServiceCollection services)
    {
        var botApiOptions = services.BuildServiceProvider().GetRequiredService<IOptions<TelegramBotApiOptions>>();
        
        var token = !string.IsNullOrEmpty(botApiOptions.Value.Token) 
            ? botApiOptions.Value.Token
            : throw new ArgumentOutOfRangeException(nameof(TelegramBotApiOptions.Token), "Token can't be empty");

        services
            .AddHttpClient(Constants.Http.TelegramBotClientName)
            .AddTypedClient<ITelegramBotClient>((httpClient, _) => new Telegram.Bot.TelegramBotClient( new TelegramBotClientOptions(token), httpClient));
        
        services
            .AddMediatR(config => config.RegisterServicesFromAssembly(typeof(ServiceCollectionExtensions).Assembly))
            .AddScoped<UpdateHandler>()
            .AddScoped<ReceiverService>()
            .AddSingleton<ChatContextManager>()
            .AddSingleton<MessageRouter>()
            .AddSingleton<CallbackQueryRouter>()
            .AddTransient<BotNavigationManager>()
            .AddHostedService<PollingService>();
        
        return services;
    } 
}
