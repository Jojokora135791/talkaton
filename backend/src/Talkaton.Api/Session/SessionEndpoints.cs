using Talkaton.Api.Common;
using Talkaton.Infrastructure.Persistence;

namespace Talkaton.Api.Session;

/// <param name="Name">Имя, оно же логин. Уникально без учёта регистра.</param>
/// <param name="TimeZoneId">IANA-зона браузера, например "Asia/Yekaterinburg".</param>
/// <param name="UtcOffsetMinutes">
/// Смещение на восток от UTC в минутах (+300 для Екатеринбурга). Нужно ровно один раз —
/// чтобы демо-неделя при первом входе легла на рабочие часы, а не на ночь.
/// </param>
public record SignInRequest(string Name, string? TimeZoneId, int? UtcOffsetMinutes);

public static class SessionEndpoints
{
    private const int MaxNameLength = 200;

    /// <summary>Разумный предел смещения часового пояса: реальные зоны лежат в ±14 часах.</summary>
    private const int MaxOffsetMinutes = 14 * 60;

    public static IEndpointRouteBuilder MapSessionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/session", async (
                SignInRequest request,
                TalkatonDbContext db,
                CancellationToken ct) =>
            {
                var name = request.Name?.Trim() ?? string.Empty;
                if (name.Length == 0)
                {
                    return Results.Problem(
                        title: "Пустое имя",
                        detail: "Введите имя — по нему календарь узнаёт вас в следующий раз.",
                        statusCode: StatusCodes.Status400BadRequest);
                }

                if (name.Length > MaxNameLength)
                {
                    return Results.Problem(
                        title: "Слишком длинное имя",
                        detail: $"Не длиннее {MaxNameLength} символов.",
                        statusCode: StatusCodes.Status400BadRequest);
                }

                var offset = Math.Clamp(request.UtcOffsetMinutes ?? 0, -MaxOffsetMinutes, MaxOffsetMinutes);
                var user = await UserWorkspaceProvisioner.EnsureAsync(
                    db,
                    name,
                    string.IsNullOrWhiteSpace(request.TimeZoneId) ? "UTC" : request.TimeZoneId.Trim(),
                    offset,
                    DateTime.UtcNow,
                    ct);

                return Results.Ok(UserDto.From(user));
            })
            .WithName("SignIn")
            .WithTags("Session")
            .WithSummary("Вход по имени: заводит рабочее пространство или возвращает уже существующее")
            .Produces<UserDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        app.MapGet("/api/session", (CurrentUser currentUser) => Results.Ok(UserDto.From(currentUser.Required)))
            .AddEndpointFilter<RequireUserFilter>()
            .WithName("GetSession")
            .WithTags("Session")
            .WithSummary("Проверка сохранённого входа при перезагрузке страницы")
            .Produces<UserDto>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }
}
