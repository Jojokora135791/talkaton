using Talkaton.Domain.Entities;
using Talkaton.Domain.Scheduling;

namespace Talkaton.Api.Tests;

public class OccurrenceCalculatorTests
{
    private static readonly DateTime FirstWednesday = new(2026, 9, 9, 6, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime WindowStart = new(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime WindowEnd = new(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Разовая_встреча_даёт_ровно_одно_вхождение()
    {
        var meeting = NewSeries(recurrence: null);

        var occurrences = OccurrenceCalculator.Expand([meeting], WindowStart, WindowEnd);

        var single = Assert.Single(occurrences);
        Assert.Equal(FirstWednesday, single.StartUtc);
        Assert.Equal(FirstWednesday, single.OccurrenceStartUtc);
        Assert.False(single.IsMoved);
    }

    [Fact]
    public void Перенос_одного_вхождения_не_трогает_соседние()
    {
        var meeting = NewSeries("FREQ=WEEKLY;BYDAY=WE");
        var second = FirstWednesday.AddDays(7);
        meeting.Overrides.Add(new EventOccurrenceOverride
        {
            Id = Guid.NewGuid(),
            EventId = meeting.Id,
            OriginalStartUtc = second,
            StartUtc = second.AddDays(1).AddHours(2),
            EndUtc = second.AddDays(1).AddHours(3),
        });

        var occurrences = OccurrenceCalculator.Expand([meeting], WindowStart, WindowEnd);

        Assert.Equal(4, occurrences.Count);
        var moved = Assert.Single(occurrences, x => x.IsMoved);
        Assert.Equal(second, moved.OccurrenceStartUtc);
        Assert.Equal(DayOfWeek.Thursday, moved.StartUtc.DayOfWeek);
        Assert.All(occurrences.Where(x => !x.IsMoved), x => Assert.Equal(DayOfWeek.Wednesday, x.StartUtc.DayOfWeek));
    }

    [Fact]
    public void Отменённое_вхождение_исчезает_из_сетки()
    {
        var meeting = NewSeries("FREQ=WEEKLY;BYDAY=WE");
        meeting.Overrides.Add(new EventOccurrenceOverride
        {
            Id = Guid.NewGuid(),
            EventId = meeting.Id,
            OriginalStartUtc = FirstWednesday.AddDays(14),
            IsCancelled = true,
        });

        var occurrences = OccurrenceCalculator.Expand([meeting], WindowStart, WindowEnd);

        Assert.Equal(3, occurrences.Count);
        Assert.DoesNotContain(occurrences, x => x.OccurrenceStartUtc == FirstWednesday.AddDays(14));
    }

    [Fact]
    public void Вхождение_перенесённое_в_окно_снаружи_всё_равно_показывается()
    {
        var meeting = NewSeries("FREQ=WEEKLY;BYDAY=WE");
        var farFuture = FirstWednesday.AddDays(70);
        meeting.Overrides.Add(new EventOccurrenceOverride
        {
            Id = Guid.NewGuid(),
            EventId = meeting.Id,
            OriginalStartUtc = farFuture,
            StartUtc = FirstWednesday.AddDays(3),
            EndUtc = FirstWednesday.AddDays(3).AddHours(1),
        });

        var occurrences = OccurrenceCalculator.Expand([meeting], WindowStart, WindowEnd);

        var moved = Assert.Single(occurrences, x => x.OccurrenceStartUtc == farFuture);
        Assert.True(moved.IsMoved);
        Assert.Equal(FirstWednesday.AddDays(3), moved.StartUtc);
    }

    [Fact]
    public void Вхождение_ищется_по_своему_ключу_даже_после_переноса()
    {
        var meeting = NewSeries("FREQ=WEEKLY;BYDAY=WE");
        var third = FirstWednesday.AddDays(14);
        meeting.Overrides.Add(new EventOccurrenceOverride
        {
            Id = Guid.NewGuid(),
            EventId = meeting.Id,
            OriginalStartUtc = third,
            StartUtc = third.AddHours(5),
            EndUtc = third.AddHours(6),
        });

        var found = OccurrenceCalculator.Find(meeting, third);

        Assert.NotNull(found);
        Assert.Equal(third.AddHours(5), found!.Value.StartUtc);
    }

    [Fact]
    public void Битое_правило_в_базе_не_роняет_сетку()
    {
        var meeting = NewSeries("FREQ=НЕТ ТАКОГО");

        var occurrences = OccurrenceCalculator.Expand([meeting], WindowStart, WindowEnd);

        var single = Assert.Single(occurrences);
        Assert.Equal(FirstWednesday, single.StartUtc);
    }

    private static Event NewSeries(string? recurrence) => new()
    {
        Id = Guid.NewGuid(),
        CalendarId = Guid.NewGuid(),
        OrganizerId = Guid.NewGuid(),
        Title = "Штаб Платформы Данных",
        StartUtc = FirstWednesday,
        EndUtc = FirstWednesday.AddMinutes(45),
        RecurrenceRule = recurrence,
    };
}
