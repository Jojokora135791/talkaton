using System.Net;
using System.Net.Http.Json;
using Talkaton.Api.Health;

namespace Talkaton.Api.Tests;

public class HealthEndpointTests(TalkatonApiFactory factory) : IClassFixture<TalkatonApiFactory>
{
    [Fact]
    public async Task Health_reports_database_as_down_when_postgres_is_unreachable()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(body);
        Assert.Equal("degraded", body!.Status);
        Assert.Equal("down", body.Database);
    }
}
