using Microsoft.EntityFrameworkCore;
using Talkaton.Infrastructure.Persistence;

namespace Talkaton.Api.Health;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", async (TalkatonDbContext db, CancellationToken ct) =>
            {
                var databaseReachable = await db.Database.CanConnectAsync(ct);

                var response = new HealthResponse(
                    Status: databaseReachable ? "healthy" : "degraded",
                    Database: databaseReachable ? "up" : "down",
                    Version: typeof(HealthEndpoints).Assembly.GetName().Version?.ToString() ?? "unknown",
                    UtcNow: DateTimeOffset.UtcNow);

                return databaseReachable
                    ? Results.Ok(response)
                    : Results.Json(response, statusCode: StatusCodes.Status503ServiceUnavailable);
            })
            .WithName("GetHealth")
            .WithTags("Health")
            .Produces<HealthResponse>()
            .Produces<HealthResponse>(StatusCodes.Status503ServiceUnavailable);

        return app;
    }
}

public record HealthResponse(string Status, string Database, string Version, DateTimeOffset UtcNow);
