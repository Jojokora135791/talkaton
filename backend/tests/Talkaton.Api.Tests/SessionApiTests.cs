using System.Net;
using System.Net.Http.Json;
using Talkaton.Api.Calendars;
using Talkaton.Api.Common;
using Talkaton.Api.Events;

namespace Talkaton.Api.Tests;

public class SessionApiTests(TalkatonApiFactory factory) : IClassFixture<TalkatonApiFactory>
{
    [Fact]
    public async Task Вход_с_тем_же_именем_возвращает_то_же_рабочее_пространство()
    {
        var first = await factory.SignInAsync("Сергей");
        var firstCalendars = await first.GetFromJsonAsync<List<CalendarDto>>("/api/calendars");
        var firstEvents = await GetWeekAsync(first);

        var second = await factory.SignInAsync("Сергей");
        var secondCalendars = await second.GetFromJsonAsync<List<CalendarDto>>("/api/calendars");
        var secondEvents = await GetWeekAsync(second);

        Assert.Equal(
            first.DefaultRequestHeaders.GetValues(CurrentUser.HeaderName),
            second.DefaultRequestHeaders.GetValues(CurrentUser.HeaderName));
        Assert.Equal(
            firstCalendars!.Select(x => x.Id),
            secondCalendars!.Select(x => x.Id));
        Assert.Equal(firstEvents.Count, secondEvents.Count);
        Assert.NotEmpty(firstEvents);
    }

    [Theory]
    [InlineData("Кирилл Соколов")]
    [InlineData("кирилл соколов")]
    [InlineData("  Кирилл Соколов  ")]
    public async Task Имя_не_чувствительно_к_регистру_и_краевым_пробелам(string name)
    {
        var canonical = await factory.SignInAsync("Кирилл Соколов");
        var again = await factory.SignInAsync(name);

        Assert.Equal(
            canonical.DefaultRequestHeaders.GetValues(CurrentUser.HeaderName),
            again.DefaultRequestHeaders.GetValues(CurrentUser.HeaderName));
    }

    [Fact]
    public async Task Разные_имена_получают_разные_календари()
    {
        var first = await factory.SignInAsync("Ирина Первая");
        var second = await factory.SignInAsync("Пётр Второй");

        var firstCalendars = await first.GetFromJsonAsync<List<CalendarDto>>("/api/calendars");
        var secondCalendars = await second.GetFromJsonAsync<List<CalendarDto>>("/api/calendars");

        Assert.Equal(4, firstCalendars!.Count);
        Assert.Equal(4, secondCalendars!.Count);
        Assert.Empty(firstCalendars.Select(x => x.Id).Intersect(secondCalendars.Select(x => x.Id)));
    }

    [Fact]
    public async Task Коллега_из_демо_данных_входит_в_собственное_пространство()
    {
        await factory.SignInAsync("Владимир Хозяин");

        // «Алина Мороз» уже заведена как участница чужих встреч, но своих календарей у неё ещё нет.
        var colleague = await factory.SignInAsync("Алина Мороз");
        var calendars = await colleague.GetFromJsonAsync<List<CalendarDto>>("/api/calendars");

        Assert.Equal(4, calendars!.Count);
    }

    [Fact]
    public async Task Пустое_имя_отклоняется()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/session", new { name = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Без_заголовка_с_пользователем_календарь_не_отдаётся()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/calendars");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task<List<OccurrenceDto>> GetWeekAsync(HttpClient client)
    {
        var (from, to) = DemoWeek.Window();
        var events = await client.GetFromJsonAsync<List<OccurrenceDto>>(
            $"/api/events?from={from:O}&to={to:O}");

        return events!;
    }
}

/// <summary>
/// Демо-неделя привязана к неделе первого входа в часовом поясе клиента —
/// тесты считают её тем же способом, что и наполнение.
/// </summary>
public static class DemoWeek
{
    public static (DateTime From, DateTime To) Window(int utcOffsetMinutes = 300)
    {
        var local = DateTime.UtcNow.AddMinutes(utcOffsetMinutes);
        var monday = local.Date.AddDays(-(((int)local.DayOfWeek + 6) % 7));
        var from = DateTime.SpecifyKind(monday.AddMinutes(-utcOffsetMinutes), DateTimeKind.Utc);

        return (from, from.AddDays(7));
    }
}
