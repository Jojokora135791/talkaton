using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Talkaton.Api.Health;

namespace Talkaton.Api.Tests;

public class HealthEndpointTests(TalkatonApiFactory factory) : IClassFixture<TalkatonApiFactory>
{
    [Theory]
    [InlineData("/health")]
    [InlineData("/api/health")]
    public async Task Здоровый_стенд_отвечает_200_на_обоих_адресах(string route)
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync(route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.Equal("healthy", body!.Status);
        Assert.Equal("up", body.Database);
    }

    [Fact]
    public async Task Недоступная_база_превращает_health_в_503()
    {
        using var broken = new UnreachableDatabaseFactory();
        var client = broken.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.Equal("degraded", body!.Status);
        Assert.Equal("down", body.Database);
    }

    /// <summary>
    /// Файл базы в несуществующем каталоге и только на чтение: SQLite такую базу
    /// не создаст и не откроет — ровно то состояние, ради которого healthcheck и нужен.
    /// </summary>
    private sealed class UnreachableDatabaseFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Database:MigrateOnStartup"] = "false",
                    ["ConnectionStrings:Sqlite"] = "Data Source=./нет-такого-каталога/talkaton.db;Mode=ReadOnly",
                }));
        }
    }
}
