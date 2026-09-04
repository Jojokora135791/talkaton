namespace Talkaton.Domain.Recurrence;

/// <summary>
/// Разворачивает серию в конкретные времена начала. Работает на чтении и ничего не пишет
/// в базу — решение из плана этапа 2.1, чтобы бесконечные серии не превращались
/// в бесконечные строки.
/// </summary>
public static class RecurrenceExpander
{
    /// <summary>
    /// Предохранитель от «FREQ=DAILY и окно в сто лет»: столько вхождений максимум
    /// перебираем от начала серии, дальше считаем, что кто-то ошибся с запросом.
    /// </summary>
    public const int MaxOccurrences = 5000;

    /// <summary>
    /// Времена начала вхождений, пересекающихся с окном [windowStartUtc, windowEndUtc).
    /// COUNT считается от начала серии, а не от начала окна — иначе правило врёт
    /// при листании календаря вперёд.
    /// </summary>
    public static IEnumerable<DateTime> Expand(
        DateTime seriesStartUtc,
        TimeSpan duration,
        RecurrenceRule rule,
        DateTime windowStartUtc,
        DateTime windowEndUtc)
    {
        if (windowEndUtc <= windowStartUtc)
        {
            yield break;
        }

        var emitted = 0;
        foreach (var start in EnumerateStarts(seriesStartUtc, rule))
        {
            if (rule.Count is { } max && emitted >= max)
            {
                yield break;
            }

            if (rule.UntilUtc is { } until && start > until)
            {
                yield break;
            }

            emitted++;
            if (emitted > MaxOccurrences)
            {
                yield break;
            }

            if (start >= windowEndUtc)
            {
                yield break;
            }

            // Встреча, начавшаяся до окна, но заходящая в него, всё равно рисуется.
            if (start + duration > windowStartUtc)
            {
                yield return start;
            }
        }
    }

    /// <summary>Все времена начала подряд, от первого и до упора. Ограничения накладывает вызывающий.</summary>
    private static IEnumerable<DateTime> EnumerateStarts(DateTime seriesStartUtc, RecurrenceRule rule) =>
        rule.Frequency switch
        {
            RecurrenceFrequency.Daily => Step(seriesStartUtc, step => step.AddDays(rule.Interval)),
            RecurrenceFrequency.Weekly => Weekly(seriesStartUtc, rule),
            RecurrenceFrequency.Monthly => ByCalendarUnit(seriesStartUtc, rule.Interval, months: true),
            RecurrenceFrequency.Yearly => ByCalendarUnit(seriesStartUtc, rule.Interval, months: false),
            _ => [],
        };

    private static IEnumerable<DateTime> Step(DateTime start, Func<DateTime, DateTime> next)
    {
        var current = start;
        while (true)
        {
            yield return current;
            current = next(current);
        }
    }

    private static IEnumerable<DateTime> Weekly(DateTime seriesStartUtc, RecurrenceRule rule)
    {
        var days = rule.ByDay.Count > 0
            ? rule.ByDay.OrderBy(WeekIndex).ToArray()
            : [seriesStartUtc.DayOfWeek];

        var timeOfDay = seriesStartUtc.TimeOfDay;
        var weekStart = seriesStartUtc.Date.AddDays(-WeekIndex(seriesStartUtc.DayOfWeek));

        while (true)
        {
            foreach (var day in days)
            {
                var candidate = weekStart.AddDays(WeekIndex(day)) + timeOfDay;
                if (candidate >= seriesStartUtc)
                {
                    yield return candidate;
                }
            }

            weekStart = weekStart.AddDays(7 * rule.Interval);
        }
    }

    /// <summary>
    /// Месяцы и годы шагаем по номеру месяца, а не прибавлением 30 дней: встреча
    /// «каждое 31-е» в коротком месяце просто пропускается, а не съезжает на 1-е.
    /// </summary>
    private static IEnumerable<DateTime> ByCalendarUnit(DateTime seriesStartUtc, int interval, bool months)
    {
        var timeOfDay = seriesStartUtc.TimeOfDay;
        var day = seriesStartUtc.Day;
        var month = seriesStartUtc.Month;
        var year = seriesStartUtc.Year;
        var stepMonths = months ? interval : interval * 12;

        var steps = 0;
        while (steps <= MaxOccurrences)
        {
            var absoluteMonth = (year * 12) + (month - 1) + (stepMonths * steps);
            var targetYear = absoluteMonth / 12;
            var targetMonth = (absoluteMonth % 12) + 1;
            steps++;

            if (targetYear > 9999)
            {
                yield break;
            }

            if (day <= DateTime.DaysInMonth(targetYear, targetMonth))
            {
                yield return new DateTime(targetYear, targetMonth, day, 0, 0, 0, DateTimeKind.Utc) + timeOfDay;
            }
        }
    }

    /// <summary>Понедельник — 0: календарь рисуется с понедельника, как на макете.</summary>
    private static int WeekIndex(DayOfWeek day) => ((int)day + 6) % 7;
}
