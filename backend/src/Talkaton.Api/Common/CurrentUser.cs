using Microsoft.EntityFrameworkCore;
using Talkaton.Domain.Entities;
using Talkaton.Infrastructure.Persistence;

namespace Talkaton.Api.Common;

/// <summary>
/// Кто сейчас работает. Этап 2 живёт без авторизации: клиент присылает id, полученный
/// при входе по имени. На этапе 6 это место заменит SSO Контура — и, по замыслу,
/// только это место, поэтому больше нигде заголовок не читается.
/// </summary>
public sealed class CurrentUser(IHttpContextAccessor accessor, TalkatonDbContext db)
{
    public const string HeaderName = "X-Talkaton-User";

    private User? resolved;

    public async ValueTask<User?> TryResolveAsync(CancellationToken ct)
    {
        if (resolved is not null)
        {
            return resolved;
        }

        var header = accessor.HttpContext?.Request.Headers[HeaderName].ToString();
        if (!Guid.TryParse(header, out var id))
        {
            return null;
        }

        resolved = await db.Users.FirstOrDefaultAsync(x => x.Id == id, ct);
        return resolved;
    }

    /// <summary>Действителен только внутри группы маршрутов с <see cref="RequireUserFilter"/>.</summary>
    public User Required => resolved
        ?? throw new InvalidOperationException(
            "Пользователь не разрешён: маршрут должен стоять за RequireUserFilter.");
}

/// <summary>Отсекает запросы без известного пользователя до попадания в обработчик.</summary>
public sealed class RequireUserFilter(CurrentUser currentUser) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var user = await currentUser.TryResolveAsync(context.HttpContext.RequestAborted);
        if (user is null)
        {
            return Results.Problem(
                title: "Не представились",
                detail: $"Заголовок {CurrentUser.HeaderName} пуст или указывает на неизвестного пользователя.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        return await next(context);
    }
}
