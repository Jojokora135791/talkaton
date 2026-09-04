using Microsoft.EntityFrameworkCore;
using Talkaton.Infrastructure.Persistence;

namespace Talkaton.Api.Health;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        // "/health" — для docker healthcheck и мониторинга,
        // "/api/health" — для фронта, у которого весь бэкенд живёт под префиксом /api.
        foreach (var route in new[] { "/health", "/api/health" })
        {
            MapHealth(app, route);
        }

        return app;
    }

    private static void MapHealth(IEndpointRouteBuilder app, string route)
    {
        app.MapGet(route, async (TalkatonDbContext db, CancellationToken ct) =>
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
            .WithName($"GetHealth{route.Replace("/", "_")}")
            .WithTags("Health")
            .Produces<HealthResponse>()
            .Produces<HealthResponse>(StatusCodes.Status503ServiceUnavailable);
    }
}

public record HealthResponse(string Status, string Database, string Version, DateTimeOffset UtcNow);
