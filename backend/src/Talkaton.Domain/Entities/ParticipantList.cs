namespace Talkaton.Domain.Entities;

/// <summary>Блок «Списки участников» из левой панели: «Команда Платформы», «Биллинг ПД».</summary>
public class ParticipantList
{
    public Guid Id { get; set; }

    public Guid OwnerId { get; set; }
    public User? Owner { get; set; }

    public required string Name { get; set; }

    public int SortOrder { get; set; }

    public ICollection<ParticipantListMember> Members { get; set; } = new List<ParticipantListMember>();
}

/// <summary>Строка списка участников. Ключ составной — (ListId, UserId).</summary>
public class ParticipantListMember
{
    public Guid ListId { get; set; }
    public ParticipantList? List { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }
}
