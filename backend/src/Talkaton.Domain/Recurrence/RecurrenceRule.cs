using System.Globalization;
using System.Text;

namespace Talkaton.Domain.Recurrence;

public enum RecurrenceFrequency
{
    Daily,
    Weekly,
    Monthly,
    Yearly,
}

/// <summary>
/// Разобранный RRULE по RFC 5545 — ровно то подмножество, которое умеет рисовать календарь:
/// FREQ, INTERVAL, BYDAY, COUNT, UNTIL. Хранится в базе строкой (см. <c>Event.RecurrenceRule</c>),
/// чтобы синхронизация с Google и ICS на этапе 3 не упиралась в самописный формат.
/// </summary>
public sealed class RecurrenceRule
{
    private static readonly (string Code, DayOfWeek Day)[] DayCodes =
    [
        ("MO", DayOfWeek.Monday),
        ("TU", DayOfWeek.Tuesday),
        ("WE", DayOfWeek.Wednesday),
        ("TH", DayOfWeek.Thursday),
        ("FR", DayOfWeek.Friday),
        ("SA", DayOfWeek.Saturday),
        ("SU", DayOfWeek.Sunday),
    ];

    private RecurrenceRule(
        RecurrenceFrequency frequency,
        int interval,
        IReadOnlyList<DayOfWeek> byDay,
        int? count,
        DateTime? untilUtc)
    {
        Frequency = frequency;
        Interval = interval;
        ByDay = byDay;
        Count = count;
        UntilUtc = untilUtc;
    }

    public RecurrenceFrequency Frequency { get; }

    /// <summary>Шаг повтора, всегда &gt;= 1.</summary>
    public int Interval { get; }

    /// <summary>Дни недели для FREQ=WEEKLY. Пустой список — берём день недели самой встречи.</summary>
    public IReadOnlyList<DayOfWeek> ByDay { get; }

    /// <summary>Сколько всего вхождений, считая от начала серии. <c>null</c> — без ограничения.</summary>
    public int? Count { get; }

    /// <summary>Дата последнего вхождения включительно, UTC. <c>null</c> — без ограничения.</summary>
    public DateTime? UntilUtc { get; }

    public static RecurrenceRule Weekly(params DayOfWeek[] days) =>
        new(RecurrenceFrequency.Weekly, 1, days, null, null);

    /// <summary>
    /// Разбирает RRULE. Возвращает <c>false</c> вместо исключения: строка приходит из HTTP,
    /// и на кривой ввод API должен отвечать 400, а не 500.
    /// </summary>
    public static bool TryParse(string? text, out RecurrenceRule? rule, out string? error)
    {
        rule = null;
        error = null;

        if (string.IsNullOrWhiteSpace(text))
        {
            error = "Пустое правило повторяемости";
            return false;
        }

        var body = text.Trim();
        if (body.StartsWith("RRULE:", StringComparison.OrdinalIgnoreCase))
        {
            body = body["RRULE:".Length..];
        }

        RecurrenceFrequency? frequency = null;
        var interval = 1;
        var byDay = new List<DayOfWeek>();
        int? count = null;
        DateTime? until = null;

        foreach (var part in body.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = part.IndexOf('=');
            if (separator <= 0)
            {
                error = $"Не разобрана часть правила: {part}";
                return false;
            }

            var name = part[..separator].ToUpperInvariant();
            var value = part[(separator + 1)..].Trim();

            switch (name)
            {
                case "FREQ":
                    frequency = value.ToUpperInvariant() switch
                    {
                        "DAILY" => RecurrenceFrequency.Daily,
                        "WEEKLY" => RecurrenceFrequency.Weekly,
                        "MONTHLY" => RecurrenceFrequency.Monthly,
                        "YEARLY" => RecurrenceFrequency.Yearly,
                        _ => null,
                    };
                    if (frequency is null)
                    {
                        error = $"Неподдерживаемая частота FREQ={value}";
                        return false;
                    }

                    break;

                case "INTERVAL":
                    if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out interval) || interval < 1)
                    {
                        error = $"INTERVAL должен быть целым числом больше нуля, получено {value}";
                        return false;
                    }

                    break;

                case "COUNT":
                    if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedCount) || parsedCount < 1)
                    {
                        error = $"COUNT должен быть целым числом больше нуля, получено {value}";
                        return false;
                    }

                    count = parsedCount;
                    break;

                case "UNTIL":
                    if (!TryParseUntil(value, out var parsedUntil))
                    {
                        error = $"UNTIL должен быть в формате 20260930T235959Z, получено {value}";
                        return false;
                    }

                    until = parsedUntil;
                    break;

                case "BYDAY":
                    foreach (var code in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        var match = Array.FindIndex(DayCodes, x => x.Code == code.ToUpperInvariant());
                        if (match < 0)
                        {
                            error = $"Неизвестный день недели в BYDAY: {code}";
                            return false;
                        }

                        if (!byDay.Contains(DayCodes[match].Day))
                        {
                            byDay.Add(DayCodes[match].Day);
                        }
                    }

                    break;

                default:
                    // Незнакомые части (WKST, BYMONTHDAY и прочее) молча пропускаем:
                    // лучше показать встречу по огрублённому правилу, чем потерять её совсем.
                    break;
            }
        }

        if (frequency is null)
        {
            error = "В правиле нет обязательной части FREQ";
            return false;
        }

        if (count is not null && until is not null)
        {
            error = "COUNT и UNTIL взаимоисключающие по RFC 5545";
            return false;
        }

        rule = new RecurrenceRule(frequency.Value, interval, byDay, count, until);
        return true;
    }

    /// <summary>Обратно в строку — так в базе всегда лежит канонический вид, а не то, что прислал клиент.</summary>
    public string ToRRule()
    {
        var builder = new StringBuilder("FREQ=");
        builder.Append(Frequency.ToString().ToUpperInvariant());

        if (Interval > 1)
        {
            builder.Append(CultureInfo.InvariantCulture, $";INTERVAL={Interval}");
        }

        if (ByDay.Count > 0)
        {
            var codes = ByDay
                .OrderBy(day => (int)(day + 6) % 7)
                .Select(day => Array.Find(DayCodes, x => x.Day == day).Code);
            builder.Append(";BYDAY=").AppendJoin(',', codes);
        }

        if (Count is { } count)
        {
            builder.Append(CultureInfo.InvariantCulture, $";COUNT={count}");
        }

        if (UntilUtc is { } until)
        {
            builder.Append(";UNTIL=").Append(until.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    private static bool TryParseUntil(string value, out DateTime untilUtc)
    {
        string[] formats = ["yyyyMMdd'T'HHmmss'Z'", "yyyyMMdd'T'HHmmss", "yyyyMMdd"];
        var parsed = DateTime.TryParseExact(
            value,
            formats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
            out untilUtc);

        if (parsed && value.Length == 8)
        {
            // Голая дата в UNTIL значит «весь этот день включительно».
            untilUtc = untilUtc.Date.AddDays(1).AddTicks(-1);
        }

        return parsed;
    }
}
