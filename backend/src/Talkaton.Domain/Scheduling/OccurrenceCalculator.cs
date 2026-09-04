using Talkaton.Domain.Entities;
using Talkaton.Domain.Recurrence;

namespace Talkaton.Domain.Scheduling;

/// <summary>
/// Превращает хранимые серии в то, что видно в сетке за выбранный период.
/// Единственное место, где встречаются RRULE и исключения вхождений.
/// </summary>
public static class OccurrenceCalculator
{
    public static IReadOnlyList<EventOccurrence> Expand(
        IEnumerable<Event> events,
        DateTime windowStartUtc,
        DateTime windowEndUtc)
    {
        var result = new List<EventOccurrence>();

        foreach (var source in events)
        {
            result.AddRange(ExpandOne(source, windowStartUtc, windowEndUtc));
        }

        return result.OrderBy(x => x.StartUtc).ThenBy(x => x.Event.Title, StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// Находит вхождение по его ключу — нужно при переносе и отмене конкретного вхождения.
    /// Возвращает <c>null</c>, если такого вхождения в серии нет или оно уже отменено.
    /// </summary>
    public static EventOccurrence? Find(Event source, DateTime occurrenceStartUtc)
    {
        // Перенесённое вхождение могло уехать от своего места в расписании как угодно далеко,
        // поэтому исключения смотрим по ключу напрямую, а не через окно развёртки.
        var patch = source.Overrides.FirstOrDefault(x => x.OriginalStartUtc == occurrenceStartUtc);
        if (patch is not null)
        {
            if (patch.IsCancelled)
            {
                return null;
            }

            var start = patch.StartUtc ?? occurrenceStartUtc;
            return new EventOccurrence(
                source,
                occurrenceStartUtc,
                start,
                patch.EndUtc ?? start + source.Duration,
                IsMoved: true);
        }

        // Вхождение на своём месте: узкого окна вокруг ключа хватает, серию целиком не разворачиваем.
        foreach (var occurrence in ExpandOne(source, occurrenceStartUtc.AddDays(-1), occurrenceStartUtc.AddDays(1)))
        {
            if (occurrence.OccurrenceStartUtc == occurrenceStartUtc)
            {
                return occurrence;
            }
        }

        return null;
    }

    private static IEnumerable<EventOccurrence> ExpandOne(Event source, DateTime windowStartUtc, DateTime windowEndUtc)
    {
        var overrides = source.Overrides.ToDictionary(x => x.OriginalStartUtc);
        var seen = new HashSet<DateTime>();

        foreach (var scheduledStart in ScheduledStarts(source, windowStartUtc, windowEndUtc))
        {
            seen.Add(scheduledStart);

            if (!overrides.TryGetValue(scheduledStart, out var patch))
            {
                yield return new EventOccurrence(source, scheduledStart, scheduledStart, scheduledStart + source.Duration, IsMoved: false);
                continue;
            }

            if (patch.IsCancelled)
            {
                continue;
            }

            var start = patch.StartUtc ?? scheduledStart;
            var end = patch.EndUtc ?? start + source.Duration;

            // Перенесённое вхождение могло уехать из окна — тогда здесь его не показываем.
            if (end > windowStartUtc && start < windowEndUtc)
            {
                yield return new EventOccurrence(source, scheduledStart, start, end, IsMoved: true);
            }
        }

        // Вхождение, перенесённое в окно снаружи, развёрткой не находится — добираем отдельно.
        // `seen` защищает от дубля: развёртка захватывает и вхождения, начавшиеся до окна.
        foreach (var patch in source.Overrides)
        {
            if (patch.IsCancelled || patch.StartUtc is not { } movedStart || seen.Contains(patch.OriginalStartUtc))
            {
                continue;
            }

            var end = patch.EndUtc ?? movedStart + source.Duration;
            if (end > windowStartUtc && movedStart < windowEndUtc)
            {
                yield return new EventOccurrence(source, patch.OriginalStartUtc, movedStart, end, IsMoved: true);
            }
        }
    }

    private static IEnumerable<DateTime> ScheduledStarts(Event source, DateTime windowStartUtc, DateTime windowEndUtc)
    {
        if (source.RecurrenceRule is null)
        {
            // Разовая встреча: одно вхождение, и оно попадает в окно, если пересекается с ним.
            if (source.EndUtc > windowStartUtc && source.StartUtc < windowEndUtc)
            {
                yield return source.StartUtc;
            }

            yield break;
        }

        if (!RecurrenceRule.TryParse(source.RecurrenceRule, out var rule, out _) || rule is null)
        {
            // Битое правило в базе не должно ронять всю сетку — показываем серию как разовую.
            if (source.EndUtc > windowStartUtc && source.StartUtc < windowEndUtc)
            {
                yield return source.StartUtc;
            }

            yield break;
        }

        foreach (var start in RecurrenceExpander.Expand(
                     source.StartUtc,
                     source.Duration,
                     rule,
                     windowStartUtc,
                     windowEndUtc))
        {
            yield return start;
        }
    }
}
