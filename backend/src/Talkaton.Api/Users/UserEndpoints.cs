using Microsoft.EntityFrameworkCore;
using Talkaton.Api.Common;
using Talkaton.Infrastructure.Persistence;

namespace Talkaton.Api.Users;

public static class UserEndpoints
{
    private const int MaxResults = 20;

    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/{id:guid}", async (
                Guid id,
                TalkatonDbContext db,
                CancellationToken ct) =>
            {
                var user = await db.Users.FirstOrDefaultAsync(x => x.Id == id, ct);
                return user is null
                    ? Results.NotFound()
                    : Results.Ok(new UserDto(user.Id, user.DisplayName, user.TimeZoneId, user.AvatarColorIndex));
            })
            .AddEndpointFilter<RequireUserFilter>()
            .WithName("GetUser")
            .WithTags("Users")
            .WithSummary("Профиль человека по id — нужен публичной странице самозаписи (Этап 7.4)")
            .Produces<UserDto>()
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        app.MapGet("/api/users", async (
                string? query,
                CurrentUser currentUser,
                TalkatonDbContext db,
                CancellationToken ct) =>
            {
                var people = db.Users.Where(x => x.Id != currentUser.Required.Id);

                if (!string.IsNullOrWhiteSpace(query))
                {
                    var needle = query.Trim().ToLowerInvariant();
                    people = people.Where(x => x.NormalizedName.Contains(needle));
                }

                var found = await people
                    .OrderBy(x => x.DisplayName)
                    .Take(MaxResults)
                    .Select(x => new UserDto(x.Id, x.DisplayName, x.TimeZoneId, x.AvatarColorIndex))
                    .ToListAsync(ct);

                return Results.Ok(found);
            })
            .AddEndpointFilter<RequireUserFilter>()
            .WithName("SearchUsers")
            .WithTags("Users")
            .WithSummary("Поиск людей для добавления в участники встречи")
            .Produces<IReadOnlyList<UserDto>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }
}
