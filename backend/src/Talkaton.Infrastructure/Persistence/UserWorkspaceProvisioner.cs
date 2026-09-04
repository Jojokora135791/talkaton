using Microsoft.EntityFrameworkCore;
using Talkaton.Domain.Entities;
using Talkaton.Domain.Recurrence;

namespace Talkaton.Infrastructure.Persistence;

/// <summary>
/// Вход по имени из плана 2.1: имя уникально, при первом входе человеку заводится
/// рабочее пространство с макета, при повторном — возвращается ровно то же самое.
/// Наполнение привязано к неделе первого входа, поэтому демо всегда выглядит «сегодняшним».
/// </summary>
public static class UserWorkspaceProvisioner
{
    /// <summary>Столько цветов аватара знает фронт (--tk-avatar-0 … --tk-avatar-5).</summary>
    private const int AvatarPaletteSize = 6;

    /// <summary>Коллеги для демо. Первые четверо — участники встречи с макета.</summary>
    private static readonly string[] Colleagues =
    [
        "Алина Мороз",
        "Денис Волков",
        "Егор Павлов",
        "Настя Крылова",
        "Павел Ковалёв",
        "Ольга Титова",
        "Максим Речкин",
        "Ирина Гущина",
    ];

    /// <summary>
    /// Находит пользователя по имени или заводит нового, а затем гарантирует,
    /// что у него есть календари и демо-неделя.
    /// </summary>
    /// <param name="utcOffsetMinutes">
    /// Смещение часового пояса клиента в минутах на восток от UTC (+300 для Екатеринбурга).
    /// Нужно, чтобы демо-встречи попали на рабочие часы, а не на ночь.
    /// </param>
    public static async Task<User> EnsureAsync(
        TalkatonDbContext db,
        string displayName,
        string timeZoneId,
        int utcOffsetMinutes,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        var user = await FindOrCreateAsync(db, displayName, timeZoneId, ct);

        if (!await db.Calendars.AnyAsync(x => x.OwnerId == user.Id, ct))
        {
            await BuildWorkspaceAsync(db, user, utcOffsetMinutes, nowUtc, ct);
        }

        return user;
    }

    private static async Task<User> FindOrCreateAsync(
        TalkatonDbContext db,
        string displayName,
        string timeZoneId,
        CancellationToken ct)
    {
        var normalized = User.Normalize(displayName);
        var existing = await db.Users.FirstOrDefaultAsync(x => x.NormalizedName == normalized, ct);
        if (existing is not null)
        {
            return existing;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            DisplayName = displayName.Trim(),
            NormalizedName = normalized,
            TimeZoneId = timeZoneId,
            AvatarColorIndex = AvatarIndex(normalized),
            CreatedUtc = DateTime.UtcNow,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return user;
    }

    /// <summary>Цвет аватара один и тот же при каждом входе — иначе люди «мигают» между сессиями.</summary>
    private static int AvatarIndex(string normalizedName)
    {
        var sum = 0;
        foreach (var symbol in normalizedName)
        {
            sum = ((sum * 31) + symbol) % 100_003;
        }

        return sum % AvatarPaletteSize;
    }

    private static async Task BuildWorkspaceAsync(
        TalkatonDbContext db,
        User owner,
        int utcOffsetMinutes,
        DateTime nowUtc,
        CancellationToken ct)
    {
        var work = NewCalendar(owner.Id, "Рабочие встречи", "#4c8dff", 0, isVisible: true);
        var personal = NewCalendar(owner.Id, "Личное", "#2fbf71", 1, isVisible: true);
        var birthdays = NewCalendar(owner.Id, "Дни рождения", "#e05572", 2, isVisible: true);
        var tasks = NewCalendar(owner.Id, "Задачи и дедлайны", "#d9a441", 3, isVisible: false);
        db.Calendars.AddRange(work, personal, birthdays, tasks);

        var team = new List<User>();
        foreach (var name in Colleagues)
        {
            var colleague = await FindOrCreateAsync(db, name, owner.TimeZoneId, ct);
            if (colleague.Id != owner.Id)
            {
                team.Add(colleague);
            }
        }

        db.ParticipantLists.AddRange(
            NewList(owner.Id, "Команда Платформы", 0, team.Take(5)),
            NewList(owner.Id, "Продуктовый штаб", 1, team.Skip(2).Take(3)),
            NewList(owner.Id, "Биллинг ПД", 2, team.Skip(4)));

        // Понедельник недели, в которую человек вошёл впервые.
        var localNow = nowUtc.AddMinutes(utcOffsetMinutes);
        var monday = localNow.Date.AddDays(-(((int)localNow.DayOfWeek + 6) % 7));

        DateTime At(int dayOffset, int hour, int minute) =>
            monday.AddDays(dayOffset).AddHours(hour).AddMinutes(minute).AddMinutes(-utcOffsetMinutes);

        var seeded = new List<Event>
        {
            Meeting(work, owner, "Синхрон с МП", At(0, 9, 30), At(0, 10, 30), "mp-sync", team.Take(3)),
            Meeting(personal, owner, "Поютречим", At(0, 11, 15), At(0, 11, 45), null, []),
            Meeting(personal, owner, "Ретро спринта", At(0, 14, 0), At(0, 15, 0), "retro-14", team.Take(4)),

            Meeting(work, owner, "Экспертиза поддержки", At(1, 10, 0), At(1, 11, 0), "support-exp", team.Skip(1).Take(2)),
            Meeting(tasks, owner, "Биллинг услуг", At(1, 13, 0), At(1, 14, 30), null, team.Skip(4).Take(2)),
            Meeting(work, owner, "ИВС. Контекст", At(1, 16, 0), At(1, 17, 0), "ivs-context", team.Take(2)),

            Meeting(personal, owner, "Cqk40nr9af68", At(2, 9, 15), At(2, 9, 45), null, []),
            Meeting(work, owner, "ИВС. Пошеринг", At(2, 13, 30), At(2, 14, 30), "ivs-share", team.Skip(2).Take(3)),
            Meeting(tasks, owner, "Синхрон с МП", At(2, 15, 30), At(2, 16, 30), "mp-sync", team.Take(3)),

            Meeting(work, owner, "Демо новой сетки", At(3, 10, 30), At(3, 11, 30), "grid-demo", team.Take(4)),
            Meeting(work, owner, "Планёрка команды", At(3, 12, 55), At(3, 14, 0), "team-plan", team, ownerStatus: ParticipantStatus.Tentative),
            Meeting(personal, owner, "1:1 с руководителем", At(3, 16, 0), At(3, 17, 30), "one-on-one", team.Take(1)),

            Meeting(work, owner, "Груминг бэклога", At(4, 11, 0), At(4, 12, 0), "backlog-groom", team.Skip(1).Take(3)),

            Meeting(personal, owner, "Личное время", At(5, 12, 0), At(5, 13, 0), null, []),
            Meeting(work, owner, "Разбор инцидента", At(6, 15, 0), At(6, 16, 0), "incident-review", team.Skip(3).Take(2)),
        };

        // Всё-дневная встреча — чтобы полоса «весь день» над сеткой не пустовала.
        var birthday = Meeting(birthdays, owner, $"День рождения — {team[3].DisplayName}", At(4, 0, 0), At(4, 23, 59), null, []);
        birthday.IsAllDay = true;
        seeded.Add(birthday);

        // Витрина макета: повторяется каждую среду, с артефактами и статусами участников.
        var headquarters = Meeting(
            work,
            owner,
            "Штаб Платформы Данных",
            At(2, 11, 0),
            At(2, 11, 45),
            "pdata-hq",
            team.Take(4),
            recurrence: RecurrenceRule.Weekly(DayOfWeek.Wednesday));

        SetStatus(headquarters, team[0], ParticipantStatus.Accepted);
        SetStatus(headquarters, team[1], ParticipantStatus.Tentative);
        SetStatus(headquarters, team[2], ParticipantStatus.Declined);
        SetStatus(headquarters, team[3], ParticipantStatus.Accepted);

        headquarters.Artifacts.Add(NewArtifact(ArtifactKind.Recording, "Запись встречи", "42 мин · доступна", 0));
        headquarters.Artifacts.Add(NewArtifact(ArtifactKind.Protocol, "Протокол совещания", "ИИ-конспект · 6 решений", 1));
        headquarters.Artifacts.Add(NewArtifact(ArtifactKind.Board, "Доска «Роадмап Q4»", "Miro-доска · 3 автора", 2));
        headquarters.Artifacts.Add(NewArtifact(ArtifactKind.Tasks, "Задачи из встречи", "4 задачи · 2 назначены", 3));

        seeded.Add(headquarters);

        db.Events.AddRange(seeded);
        await db.SaveChangesAsync(ct);
    }

    private static Calendar NewCalendar(Guid ownerId, string name, string color, int sortOrder, bool isVisible) => new()
    {
        Id = Guid.NewGuid(),
        OwnerId = ownerId,
        Name = name,
        Color = color,
        SortOrder = sortOrder,
        IsVisible = isVisible,
    };

    private static ParticipantList NewList(Guid ownerId, string name, int sortOrder, IEnumerable<User> members)
    {
        var list = new ParticipantList
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Name = name,
            SortOrder = sortOrder,
        };

        foreach (var member in members)
        {
            list.Members.Add(new ParticipantListMember { ListId = list.Id, UserId = member.Id });
        }

        return list;
    }

    private static Event Meeting(
        Calendar calendar,
        User organizer,
        string title,
        DateTime startUtc,
        DateTime endUtc,
        string? talkRoomSlug,
        IEnumerable<User> participants,
        RecurrenceRule? recurrence = null,
        ParticipantStatus ownerStatus = ParticipantStatus.Accepted)
    {
        var meeting = new Event
        {
            Id = Guid.NewGuid(),
            CalendarId = calendar.Id,
            OrganizerId = organizer.Id,
            Title = title,
            StartUtc = startUtc,
            EndUtc = endUtc,
            TalkRoomSlug = talkRoomSlug,
            RecurrenceRule = recurrence?.ToRRule(),
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow,
        };

        meeting.Participants.Add(new EventParticipant
        {
            EventId = meeting.Id,
            UserId = organizer.Id,
            Status = ownerStatus,
            IsOrganizer = true,
        });

        meeting.Reminders.Add(new Reminder { EventId = meeting.Id, UserId = organizer.Id, MinutesBefore = 10 });

        foreach (var participant in participants)
        {
            meeting.Participants.Add(new EventParticipant
            {
                EventId = meeting.Id,
                UserId = participant.Id,
                Status = ParticipantStatus.Accepted,
            });
        }

        return meeting;
    }

    private static void SetStatus(Event meeting, User user, ParticipantStatus status)
    {
        var participant = meeting.Participants.FirstOrDefault(x => x.UserId == user.Id);
        if (participant is not null)
        {
            participant.Status = status;
        }
    }

    private static EventArtifact NewArtifact(ArtifactKind kind, string title, string subtitle, int sortOrder) => new()
    {
        Id = Guid.NewGuid(),
        Kind = kind,
        Title = title,
        Subtitle = subtitle,
        SortOrder = sortOrder,
    };
}
