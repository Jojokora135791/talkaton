using Microsoft.EntityFrameworkCore;
using Talkaton.Api.Common;
using Talkaton.Domain.Entities;
using Talkaton.Infrastructure.Persistence;

namespace Talkaton.Api.Calendars;

public record CalendarDto(Guid Id, string Name, string Color, bool IsVisible, int SortOrder)
{
    public static CalendarDto From(Calendar calendar) =>
        new(calendar.Id, calendar.Name, calendar.Color, calendar.IsVisible, calendar.SortOrder);
}

/// <summary>Все поля необязательные: галочка видимости шлёт только <c>isVisible</c>.</summary>
public record UpdateCalendarRequest(string? Name, string? Color, bool? IsVisible);

public static class CalendarEndpoints
{
    public static IEndpointRouteBuilder MapCalendarEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/calendars")
            .AddEndpointFilter<RequireUserFilter>()
            .WithTags("Calendars");

        group.MapGet("/", async (CurrentUser currentUser, TalkatonDbContext db, CancellationToken ct) =>
            {
                var calendars = await db.Calendars
                    .Where(x => x.OwnerId == currentUser.Required.Id)
                    .OrderBy(x => x.SortOrder)
                    // Проекция расписана руками: статический From в дерево выражений не переводится.
                    .Select(x => new CalendarDto(x.Id, x.Name, x.Color, x.IsVisible, x.SortOrder))
                    .ToListAsync(ct);

                return Results.Ok(calendars);
            })
            .WithName("GetCalendars")
            .WithSummary("Блок «Мои календари» левой панели")
            .Produces<IReadOnlyList<CalendarDto>>();

        group.MapPatch("/{id:guid}", async (
                Guid id,
                UpdateCalendarRequest request,
                CurrentUser currentUser,
                TalkatonDbContext db,
                CancellationToken ct) =>
            {
                var calendar = await db.Calendars
                    .FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == currentUser.Required.Id, ct);

                if (calendar is null)
                {
                    return Results.NotFound();
                }

                if (request.Name is { } name)
                {
                    var trimmed = name.Trim();
                    if (trimmed.Length == 0)
                    {
                        return Results.Problem(
                            title: "Пустое название календаря",
                            statusCode: StatusCodes.Status400BadRequest);
                    }

                    calendar.Name = trimmed;
                }

                if (request.Color is { } color)
                {
                    calendar.Color = color.Trim();
                }

                if (request.IsVisible is { } isVisible)
                {
                    calendar.IsVisible = isVisible;
                }

                await db.SaveChangesAsync(ct);
                return Results.Ok(CalendarDto.From(calendar));
            })
            .WithName("UpdateCalendar")
            .WithSummary("Переключение видимости и переименование календаря")
            .Produces<CalendarDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }
}
