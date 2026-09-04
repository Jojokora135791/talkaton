namespace Talkaton.Domain.Entities;

/// <summary>
/// Персональное напоминание «за 5/10/15 минут». У каждого участника своё, поэтому
/// это отдельная строка, а не поле встречи. Планировщик живёт во фронте (этап 2.4),
/// бэкенд только хранит настройку. Ключ составной — (EventId, UserId).
/// </summary>
public class Reminder
{
    public Guid EventId { get; set; }
    public Event? Event { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    /// <summary>За сколько минут до начала предупредить. 0 — напоминание выключено.</summary>
    public int MinutesBefore { get; set; }
}
