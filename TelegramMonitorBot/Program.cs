using TelegramMonitorBot.AmazonSecretsManagerClient;
using TelegramMonitorBot.Storage;
using TelegramMonitorBot.Storage.Repositories.Abstractions;
using TelegramMonitorBot.TelegramApiClient;
using TelegramMonitorBot.TelegramBotClient;

var builder = WebApplication.CreateBuilder(args);


builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true, reloadOnChange: false)
    .AddJsonFile($"appsettings.local.json", optional: true, reloadOnChange: false)
    .AddUserSecrets<Program>();

var configuration = builder.Configuration;

builder.Services.AddSecretManagerClient(configuration);
    
builder.Host.ConfigureAppConfiguration((buildContext, configurationBuilder) =>
{
    configurationBuilder.AddAmazonSecretsManager(buildContext.Configuration);
});

builder.Services
    .ConfigureAmazonSecrets(configuration)
    .AddTelegramApiClient()
    .AddTelegramBotClient()
    .AddStorage()
    .AddControllers();

builder.Services
    .AddEndpointsApiExplorer()
    .AddSwaggerGen();

var app = builder.Build();

//
// var client = app.Services.CreateScope().ServiceProvider.GetRequiredService<ITelegramApiClient>();
// await client.DoStuff();

// var client = app.Services.CreateScope().ServiceProvider.GetRequiredService<ITelegramRepository>();
// var channels1 = await client.GetChannels(3);
// var channels2 = await client.GetChannels(3);

await app.MigrateStorage();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/api/hello", () => "Hello, World!");

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
