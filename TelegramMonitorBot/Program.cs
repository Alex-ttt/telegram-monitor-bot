using TelegramMonitorBot.AmazonSecretsManagerClient;
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


await app.MigrateStorage();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/api/hello", () => "Hello, World!");

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

//
var client = app.Services.CreateScope().ServiceProvider.GetRequiredService<ITelegramApiClient>();
// var c1 = await client.FindChannelByName("igrapoisk");
// var c2 = await client.GetChannel(-1001498653424);
// //
// var client = app.Services.CreateScope().ServiceProvider.GetRequiredService<ITelegramRepository>();

// var channels1 = await client.GetChannels(3, new Pager());
// var channels2 = await client.GetChannels(3);

app.Run();
