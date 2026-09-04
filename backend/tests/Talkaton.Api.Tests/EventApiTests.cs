using System.Net;
using System.Net.Http.Json;
using Talkaton.Api.Calendars;
using Talkaton.Api.Events;

namespace Talkaton.Api.Tests;

public class EventApiTests(TalkatonApiFactory factory) : IClassFixture<TalkatonApiFactory>
{
    private const string Showcase = "Штаб Платформы Данных";

    [Fact]
    public async Task Демо_неделя_рисуется_сразу_после_входа()
    {
        var client = await factory.SignInAsync("Демо Первый");

        var week = await GetWeekAsync(client);

        Assert.NotEmpty(week);
        Assert.Contains(week, x => x.Title == Showcase);
        Assert.Contains(week, x => x.IsAllDay);
        Assert.All(week, x => Assert.True(x.EndUtc > x.StartUtc));
    }

    [Fact]
    public async Task Витринная_встреча_повторяется_и_на_следующей_неделе()
    {
        var client = await factory.SignInAsync("Демо Повтор");
        var (from, to) = DemoWeek.Window();

        var nextWeek = await GetAsync(client, from.AddDays(7), to.AddDays(7));

        var headquarters = Assert.Single(nextWeek, x => x.Title == Showcase);
        Assert.Equal("FREQ=WEEKLY;BYDAY=WE", headquarters.RecurrenceRule);
        Assert.Equal(4, headquarters.ArtifactCount);
        Assert.Equal(5, headquarters.ParticipantCount);
    }

    [Fact]
    public async Task Созданная_встреча_появляется_в_сетке()
    {
        var client = await factory.SignInAsync("Автор Встречи");
        var calendar = await FirstCalendarAsync(client);
        var (from, _) = DemoWeek.Window();
        var start = from.AddDays(1).AddHours(9);

        var created = await CreateAsync(client, calendar.Id, "Ретро по этапу 2", start, start.AddHours(1));

        Assert.Equal("Ретро по этапу 2", created.Occurrence.Title);
        Assert.Equal(calendar.Color, created.Occurrence.CalendarColor);
        Assert.True(created.Occurrence.IsOrganizer);

        var week = await GetWeekAsync(client);
        Assert.Contains(week, x => x.EventId == created.Occurrence.EventId && x.StartUtc == start);
    }

    [Fact]
    public async Task Перенос_одного_вхождения_не_двигает_остальную_серию()
    {
        var client = await factory.SignInAsync("Тащит Мышкой");
        var (from, to) = DemoWeek.Window();
        var thisWeek = await GetAsync(client, from, to);
        var occurrence = thisWeek.Single(x => x.Title == Showcase);

        var moved = occurrence.StartUtc.AddDays(1).AddHours(2);
        var response = await client.PatchAsJsonAsync(
            $"/api/events/{occurrence.EventId}?scope=occurrence&occurrenceStart={occurrence.OccurrenceStartUtc:O}",
            new { startUtc = moved, endUtc = moved.AddMinutes(45) });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var patched = await GetAsync(client, from, to);
        var here = Assert.Single(patched, x => x.EventId == occurrence.EventId);
        Assert.Equal(moved, here.StartUtc);
        Assert.True(here.IsMoved);
        Assert.Equal(occurrence.OccurrenceStartUtc, here.OccurrenceStartUtc);

        // Следующая среда осталась на своём месте — сдвинулось ровно одно вхождение.
        var nextWeek = await GetAsync(client, from.AddDays(7), to.AddDays(7));
        var untouched = Assert.Single(nextWeek, x => x.EventId == occurrence.EventId);
        Assert.False(untouched.IsMoved);
        Assert.Equal(occurrence.StartUtc.AddDays(7), untouched.StartUtc);
    }

    [Fact]
    public async Task Удаление_вхождения_убирает_только_его()
    {
        var client = await factory.SignInAsync("Удаляет Одно");
        var (from, to) = DemoWeek.Window();
        var occurrence = (await GetAsync(client, from, to)).Single(x => x.Title == Showcase);

        var response = await client.DeleteAsync(
            $"/api/events/{occurrence.EventId}?scope=occurrence&occurrenceStart={occurrence.OccurrenceStartUtc:O}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.DoesNotContain(await GetAsync(client, from, to), x => x.EventId == occurrence.EventId);
        Assert.Contains(await GetAsync(client, from.AddDays(7), to.AddDays(7)), x => x.EventId == occurrence.EventId);
    }

    [Fact]
    public async Task Удаление_серии_убирает_её_целиком()
    {
        var client = await factory.SignInAsync("Удаляет Серию");
        var (from, to) = DemoWeek.Window();
        var occurrence = (await GetAsync(client, from, to)).Single(x => x.Title == Showcase);

        var response = await client.DeleteAsync($"/api/events/{occurrence.EventId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.DoesNotContain(await GetAsync(client, from.AddDays(7), to.AddDays(7)), x => x.EventId == occurrence.EventId);
    }

    [Fact]
    public async Task Ответ_на_приглашение_меняет_статус_участника()
    {
        var client = await factory.SignInAsync("Отвечает Нет");
        var (from, to) = DemoWeek.Window();
        var occurrence = (await GetAsync(client, from, to)).Single(x => x.Title == Showcase);

        var response = await client.PostAsJsonAsync(
            $"/api/events/{occurrence.EventId}/rsvp",
            new { status = "declined" });

        response.EnsureSuccessStatusCode();
        var details = await response.Content.ReadFromJsonAsync<EventDetailsDto>();

        Assert.Equal("declined", details!.Occurrence.MyStatus);
        Assert.Contains(details.Participants, x => x.IsOrganizer && x.Status == "declined");
    }

    [Fact]
    public async Task Неизвестный_ответ_на_приглашение_отклоняется()
    {
        var client = await factory.SignInAsync("Отвечает Странно");
        var (from, to) = DemoWeek.Window();
        var occurrence = (await GetAsync(client, from, to)).First();

        var response = await client.PostAsJsonAsync(
            $"/api/events/{occurrence.EventId}/rsvp",
            new { status = "может быть" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Фильтр_по_календарям_скрывает_чужие_встречи()
    {
        var client = await factory.SignInAsync("Прячет Календарь");
        var (from, to) = DemoWeek.Window();
        var calendars = await client.GetFromJsonAsync<List<CalendarDto>>("/api/calendars");
        var work = calendars!.Single(x => x.Name == "Рабочие встречи");

        var onlyWork = await client.GetFromJsonAsync<List<OccurrenceDto>>(
            $"/api/events?from={from:O}&to={to:O}&calendarIds={work.Id}");

        Assert.NotEmpty(onlyWork!);
        Assert.All(onlyWork!, x => Assert.Equal(work.Id, x.CalendarId));
    }

    [Fact]
    public async Task Галочка_видимости_календаря_сохраняется()
    {
        var client = await factory.SignInAsync("Щёлкает Галочкой");
        var calendars = await client.GetFromJsonAsync<List<CalendarDto>>("/api/calendars");
        var personal = calendars!.Single(x => x.Name == "Личное");

        var response = await client.PatchAsJsonAsync($"/api/calendars/{personal.Id}", new { isVisible = false });
        response.EnsureSuccessStatusCode();

        var reloaded = await client.GetFromJsonAsync<List<CalendarDto>>("/api/calendars");
        Assert.False(reloaded!.Single(x => x.Id == personal.Id).IsVisible);
    }

    [Fact]
    public async Task Артефакты_витринной_встречи_отдаются_отдельным_эндпоинтом()
    {
        var client = await factory.SignInAsync("Смотрит Артефакты");
        var (from, to) = DemoWeek.Window();
        var occurrence = (await GetAsync(client, from, to)).Single(x => x.Title == Showcase);

        var artifacts = await client.GetFromJsonAsync<List<ArtifactDto>>($"/api/events/{occurrence.EventId}/artifacts");

        Assert.Equal(
            new[] { "recording", "protocol", "board", "tasks" },
            artifacts!.Select(x => x.Kind));
    }

    [Fact]
    public async Task Повторяемость_с_кривым_правилом_не_создаётся()
    {
        var client = await factory.SignInAsync("Пишет Кривой RRULE");
        var calendar = await FirstCalendarAsync(client);
        var start = DateTime.UtcNow.Date.AddHours(9);

        var response = await client.PostAsJsonAsync("/api/events", new
        {
            calendarId = calendar.Id,
            title = "Каждый високосный вторник",
            startUtc = start,
            endUtc = start.AddHours(1),
            recurrenceRule = "FREQ=FORTNIGHTLY",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Встреча_с_концом_раньше_начала_не_создаётся()
    {
        var client = await factory.SignInAsync("Путает Границы");
        var calendar = await FirstCalendarAsync(client);
        var start = DateTime.UtcNow.Date.AddHours(9);

        var response = await client.PostAsJsonAsync("/api/events", new
        {
            calendarId = calendar.Id,
            title = "Встреча наоборот",
            startUtc = start,
            endUtc = start.AddHours(-1),
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Приглашённый_видит_встречу_но_не_правит_её()
    {
        var organizer = await factory.SignInAsync("Организатор Общей");
        var guest = await factory.SignInAsync("Гость Общей");
        var guestId = await UserIdAsync(guest);

        var calendar = await FirstCalendarAsync(organizer);
        var (from, to) = DemoWeek.Window();
        var start = from.AddDays(2).AddHours(8);

        var created = await CreateAsync(
            organizer,
            calendar.Id,
            "Общая встреча двух пространств",
            start,
            start.AddHours(1),
            participantIds: [guestId]);

        var guestWeek = await GetAsync(guest, from, to);
        var seen = Assert.Single(guestWeek, x => x.EventId == created.Occurrence.EventId);
        Assert.False(seen.IsOrganizer);
        Assert.Equal("tentative", seen.MyStatus);

        var forbidden = await guest.PatchAsJsonAsync(
            $"/api/events/{created.Occurrence.EventId}",
            new { title = "Переименовал чужое" });

        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task Приглашённый_настраивает_себе_напоминание()
    {
        var organizer = await factory.SignInAsync("Организатор Напоминаний");
        var guest = await factory.SignInAsync("Гость Напоминаний");
        var guestId = await UserIdAsync(guest);

        var calendar = await FirstCalendarAsync(organizer);
        var start = DateTime.UtcNow.Date.AddDays(1).AddHours(10);
        var created = await CreateAsync(
            organizer,
            calendar.Id,
            "Встреча с напоминанием",
            start,
            start.AddHours(1),
            participantIds: [guestId],
            reminderMinutesBefore: 15);

        var response = await guest.PatchAsJsonAsync(
            $"/api/events/{created.Occurrence.EventId}",
            new { reminderMinutesBefore = 5 });

        response.EnsureSuccessStatusCode();
        var details = await response.Content.ReadFromJsonAsync<EventDetailsDto>();

        Assert.Equal(5, details!.Occurrence.ReminderMinutesBefore);

        // У организатора своё напоминание — чужая настройка его не трогает.
        var organizerView = await organizer.GetFromJsonAsync<EventDetailsDto>(
            $"/api/events/{created.Occurrence.EventId}");
        Assert.Equal(15, organizerView!.Occurrence.ReminderMinutesBefore);
    }

    [Fact]
    public async Task Тумблер_записи_заводит_артефакты_и_при_создании_и_при_правке()
    {
        var client = await factory.SignInAsync("Включает Протокол");
        var calendar = await FirstCalendarAsync(client);
        var start = DateTime.UtcNow.Date.AddDays(1).AddHours(11);

        var created = await CreateAsync(client, calendar.Id, "Без записи", start, start.AddHours(1));
        var eventId = created.Occurrence.EventId;

        var before = await client.GetFromJsonAsync<List<ArtifactDto>>($"/api/events/{eventId}/artifacts");
        Assert.Empty(before!);

        var response = await client.PatchAsJsonAsync($"/api/events/{eventId}", new { generateArtifacts = true });
        response.EnsureSuccessStatusCode();

        var after = await client.GetFromJsonAsync<List<ArtifactDto>>($"/api/events/{eventId}/artifacts");
        Assert.Equal(new[] { "recording", "protocol" }, after!.Select(x => x.Kind));

        // Повторное включение не плодит дубли.
        (await client.PatchAsJsonAsync($"/api/events/{eventId}", new { generateArtifacts = true }))
            .EnsureSuccessStatusCode();
        var again = await client.GetFromJsonAsync<List<ArtifactDto>>($"/api/events/{eventId}/artifacts");
        Assert.Equal(2, again!.Count);
    }

    [Fact]
    public async Task Слишком_широкий_период_отклоняется()
    {
        var client = await factory.SignInAsync("Просит Век");
        var from = DateTime.UtcNow.Date;

        var response = await client.GetAsync($"/api/events?from={from:O}&to={from.AddYears(50):O}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<CalendarDto> FirstCalendarAsync(HttpClient client)
    {
        var calendars = await client.GetFromJsonAsync<List<CalendarDto>>("/api/calendars");
        return calendars!.First();
    }

    private static async Task<Guid> UserIdAsync(HttpClient client)
    {
        var me = await client.GetFromJsonAsync<Common.UserDto>("/api/session");
        return me!.Id;
    }

    private static async Task<EventDetailsDto> CreateAsync(
        HttpClient client,
        Guid calendarId,
        string title,
        DateTime startUtc,
        DateTime endUtc,
        Guid[]? participantIds = null,
        int? reminderMinutesBefore = null)
    {
        var response = await client.PostAsJsonAsync("/api/events", new
        {
            calendarId,
            title,
            startUtc,
            endUtc,
            participantIds,
            reminderMinutesBefore,
        });

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EventDetailsDto>())!;
    }

    private static Task<List<OccurrenceDto>> GetWeekAsync(HttpClient client)
    {
        var (from, to) = DemoWeek.Window();
        return GetAsync(client, from, to);
    }

    private static async Task<List<OccurrenceDto>> GetAsync(HttpClient client, DateTime from, DateTime to)
    {
        var events = await client.GetFromJsonAsync<List<OccurrenceDto>>($"/api/events?from={from:O}&to={to:O}");
        return events!;
    }
}
