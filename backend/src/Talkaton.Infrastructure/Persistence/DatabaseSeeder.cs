using Microsoft.EntityFrameworkCore;
using Talkaton.Domain.Entities;

namespace Talkaton.Infrastructure.Persistence;

/// <summary>
/// Прогретые данные для локального стенда: демо-пользователь и четыре календаря с макета.
/// Идемпотентен — можно звать на каждом старте.
/// </summary>
public static class DatabaseSeeder
{
    private static readonly Guid DemoUserId = Guid.Parse("00000000-0000-0000-0000-0000000000d1");

    public static async Task SeedAsync(TalkatonDbContext db, CancellationToken ct = default)
    {
        if (await db.Users.AnyAsync(ct))
        {
            return;
        }

        db.Users.Add(new User
        {
            Id = DemoUserId,
            DisplayName = "Кирилл Соколов",
            Email = "demo@talkaton.local",
            TimeZoneId = "Asia/Yekaterinburg",
        });

        db.Calendars.AddRange(
            NewCalendar("Рабочие встречи", "#4c8dff", 0, isVisible: true),
            NewCalendar("Личное", "#2fbf71", 1, isVisible: true),
            NewCalendar("Дни рождения", "#e05572", 2, isVisible: true),
            NewCalendar("Задачи и дедлайны", "#d9a441", 3, isVisible: false));

        await db.SaveChangesAsync(ct);
    }

    private static Calendar NewCalendar(string name, string color, int sortOrder, bool isVisible) => new()
    {
        Id = Guid.NewGuid(),
        OwnerId = DemoUserId,
        Name = name,
        Color = color,
        SortOrder = sortOrder,
        IsVisible = isVisible,
    };
}
