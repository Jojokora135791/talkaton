using Microsoft.EntityFrameworkCore;
using Talkaton.Infrastructure.Persistence;

namespace Talkaton.Api.Health;

/// <summary>
/// Миграции и seed на старте: локальный стенд должен подниматься одной командой,
/// без ручного `dotnet ef database update`.
/// </summary>
public static class DatabaseStartup
{
    public static async Task ApplyAsync(WebApplication app)
    {
        var config = app.Configuration.GetSection("Database");
        if (!config.GetValue("MigrateOnStartup", false))
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TalkatonDbContext>();
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DatabaseStartup));

        await db.Database.MigrateAsync();
        logger.LogInformation("Миграции применены");

        if (config.GetValue("SeedOnStartup", false))
        {
            await DatabaseSeeder.SeedAsync(db);
            logger.LogInformation("Seed выполнен");
        }
    }
}
