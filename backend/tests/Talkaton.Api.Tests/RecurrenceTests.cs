using Talkaton.Domain.Recurrence;

namespace Talkaton.Api.Tests;

public class RecurrenceTests
{
    private static readonly DateTime WednesdayElevenUtc = new(2026, 9, 9, 6, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Разбирает_правило_встречи_с_макета()
    {
        Assert.True(RecurrenceRule.TryParse("FREQ=WEEKLY;BYDAY=WE", out var rule, out var error));

        Assert.Null(error);
        Assert.NotNull(rule);
        Assert.Equal(RecurrenceFrequency.Weekly, rule!.Frequency);
        Assert.Equal(1, rule.Interval);
        Assert.Equal(new[] { DayOfWeek.Wednesday }, rule.ByDay);
    }

    [Theory]
    [InlineData("FREQ=WEEKLY;BYDAY=WE")]
    [InlineData("RRULE:FREQ=WEEKLY;BYDAY=WE")]
    [InlineData("freq=weekly;byday=we")]
    public void Приводит_правило_к_каноническому_виду(string input)
    {
        Assert.True(RecurrenceRule.TryParse(input, out var rule, out _));

        Assert.Equal("FREQ=WEEKLY;BYDAY=WE", rule!.ToRRule());
    }

    [Theory]
    [InlineData("")]
    [InlineData("FREQ=HOURLY")]
    [InlineData("INTERVAL=2")]
    [InlineData("FREQ=WEEKLY;BYDAY=XX")]
    [InlineData("FREQ=DAILY;COUNT=3;UNTIL=20261231T000000Z")]
    public void Кривое_правило_не_кидает_исключение_а_возвращает_ошибку(string input)
    {
        Assert.False(RecurrenceRule.TryParse(input, out var rule, out var error));

        Assert.Null(rule);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void Еженедельная_серия_разворачивается_на_каждую_среду_окна()
    {
        RecurrenceRule.TryParse("FREQ=WEEKLY;BYDAY=WE", out var rule, out _);

        var starts = RecurrenceExpander.Expand(
            WednesdayElevenUtc,
            TimeSpan.FromMinutes(45),
            rule!,
            WednesdayElevenUtc.Date,
            WednesdayElevenUtc.Date.AddDays(28)).ToList();

        Assert.Equal(4, starts.Count);
        Assert.All(starts, start => Assert.Equal(DayOfWeek.Wednesday, start.DayOfWeek));
        Assert.All(starts, start => Assert.Equal(WednesdayElevenUtc.TimeOfDay, start.TimeOfDay));
        Assert.Equal(7, (starts[1] - starts[0]).TotalDays);
    }

    [Fact]
    public void Правило_с_двумя_днями_недели_даёт_две_встречи_в_неделю()
    {
        RecurrenceRule.TryParse("FREQ=WEEKLY;BYDAY=MO,TH", out var rule, out _);

        var starts = RecurrenceExpander.Expand(
            new DateTime(2026, 9, 7, 6, 0, 0, DateTimeKind.Utc),
            TimeSpan.FromHours(1),
            rule!,
            new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 21, 0, 0, 0, DateTimeKind.Utc)).ToList();

        Assert.Equal(
            new[] { DayOfWeek.Monday, DayOfWeek.Thursday, DayOfWeek.Monday, DayOfWeek.Thursday },
            starts.Select(x => x.DayOfWeek));
    }

    [Fact]
    public void COUNT_считается_от_начала_серии_а_не_от_начала_окна()
    {
        RecurrenceRule.TryParse("FREQ=WEEKLY;BYDAY=WE;COUNT=2", out var rule, out _);

        // Окно начинается через две недели — к этому моменту серия уже исчерпана.
        var starts = RecurrenceExpander.Expand(
            WednesdayElevenUtc,
            TimeSpan.FromMinutes(45),
            rule!,
            WednesdayElevenUtc.AddDays(14),
            WednesdayElevenUtc.AddDays(60)).ToList();

        Assert.Empty(starts);
    }

    [Fact]
    public void UNTIL_обрезает_серию_по_дате()
    {
        RecurrenceRule.TryParse("FREQ=DAILY;UNTIL=20260912T235959Z", out var rule, out _);

        var starts = RecurrenceExpander.Expand(
            new DateTime(2026, 9, 9, 6, 0, 0, DateTimeKind.Utc),
            TimeSpan.FromHours(1),
            rule!,
            new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc)).ToList();

        Assert.Equal(4, starts.Count);
        Assert.Equal(new DateTime(2026, 9, 12, 6, 0, 0, DateTimeKind.Utc), starts[^1]);
    }

    [Fact]
    public void Ежемесячная_серия_пропускает_месяцы_без_нужного_числа()
    {
        RecurrenceRule.TryParse("FREQ=MONTHLY", out var rule, out _);

        var starts = RecurrenceExpander.Expand(
            new DateTime(2026, 1, 31, 9, 0, 0, DateTimeKind.Utc),
            TimeSpan.FromHours(1),
            rule!,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc)).ToList();

        // Февраля и апреля с 31-м числом не существует — они выпадают, а не съезжают на 1-е.
        Assert.Equal(
            new[] { new DateTime(2026, 1, 31, 9, 0, 0, DateTimeKind.Utc), new DateTime(2026, 3, 31, 9, 0, 0, DateTimeKind.Utc) },
            starts);
    }

    [Fact]
    public void Встреча_начавшаяся_до_окна_но_заходящая_в_него_попадает_в_выдачу()
    {
        RecurrenceRule.TryParse("FREQ=DAILY", out var rule, out _);
        var start = new DateTime(2026, 9, 9, 23, 0, 0, DateTimeKind.Utc);

        var starts = RecurrenceExpander.Expand(
            start,
            TimeSpan.FromHours(2),
            rule!,
            new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc)).ToList();

        Assert.Contains(start, starts);
    }
}
