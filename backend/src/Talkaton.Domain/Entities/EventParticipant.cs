namespace Talkaton.Domain.Entities;

/// <summary>Статусы с макета: «идёт», «возможно», «не идёт».</summary>
public enum ParticipantStatus
{
    Tentative = 0,
    Accepted = 1,
    Declined = 2,
}

/// <summary>Участник встречи. Ключ составной — (EventId, UserId).</summary>
public class EventParticipant
{
    public Guid EventId { get; set; }
    public Event? Event { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public ParticipantStatus Status { get; set; } = ParticipantStatus.Tentative;

    /// <summary>Организатор тоже участник — так правая панель обходится без спецкейса.</summary>
    public bool IsOrganizer { get; set; }
}
