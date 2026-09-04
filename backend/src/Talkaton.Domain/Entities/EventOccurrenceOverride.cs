namespace Talkaton.Domain.Entities;

/// <summary>
/// Правка одного вхождения повторяющейся серии: перенос drag-and-drop, resize или отмена.
/// Развёртка повторов идёт на чтении, поэтому выпавшие из ритма вхождения хранятся
/// отдельными строками, а не копией всей серии.
/// </summary>
public class EventOccurrenceOverride
{
    public Guid Id { get; set; }

    public Guid EventId { get; set; }
    public Event? Event { get; set; }

    /// <summary>Начало вхождения по расписанию — ключ, по которому исключение находит своё место.</summary>
    public DateTime OriginalStartUtc { get; set; }

    /// <summary>Новое начало. <c>null</c> — время не менялось, вхождение только отменено.</summary>
    public DateTime? StartUtc { get; set; }

    public DateTime? EndUtc { get; set; }

    /// <summary>Вхождение выброшено из серии — в сетке не показывается.</summary>
    public bool IsCancelled { get; set; }
}
