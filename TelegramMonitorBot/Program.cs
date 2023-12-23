using TelegramMonitorBot.AmazonSecretsManagerClient;
using TelegramMonitorBot.DynamoDBMigrator;
using TelegramMonitorBot.Storage;
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

var migrator = app.Services.GetRequiredService<StorageMigrator>();
await migrator.MigrateStorage();

//
// var client = app.Services.CreateScope().ServiceProvider.GetRequiredService<ITelegramApiClient>();
// await client.DoStuff();

// var client = app.Services.CreateScope().ServiceProvider.GetRequiredService<ITelegramRepository>();
// await client.PutUserChannel(
//     new User(3, "Oleg"),
//     new Channel(2, "Cool Channel"));

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/api/hello", () => "Hello, World!");

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
