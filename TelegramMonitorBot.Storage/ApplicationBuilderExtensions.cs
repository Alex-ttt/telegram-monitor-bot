using TelegramMonitorBot.DynamoDBMigrator;

namespace TelegramMonitorBot.Storage;

public static class ApplicationBuilderExtensions
{
    public static async Task MigrateStorage(this WebApplication app)
    {
        var migrator = app.Services.GetRequiredService<StorageMigrator>();
        await migrator.MigrateStorage();
    }
}