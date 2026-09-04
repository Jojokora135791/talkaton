using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Talkaton.Api.Tests;

/// <summary>
/// Поднимает Api без миграций и seed — тестам не нужен живой Postgres.
/// </summary>
public class TalkatonApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Database:MigrateOnStartup"] = "false",
                ["Database:SeedOnStartup"] = "false",
                ["ConnectionStrings:Postgres"] =
                    "Host=127.0.0.1;Port=1;Database=talkaton;Username=talkaton;Password=talkaton;Timeout=1",
            }));
    }
}
