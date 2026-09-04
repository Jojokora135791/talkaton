namespace Talkaton.Domain.Entities;

/// <summary>Карточки блока «Артефакты встречи» на макете.</summary>
public enum ArtifactKind
{
    Recording = 0,
    Protocol = 1,
    Board = 2,
    Tasks = 3,
}

/// <summary>
/// Артефакт встречи. На этапе 2 приезжает из наполнения демо-данными, на этапе 3 —
/// из мока Толка через ITalkGateway. Поэтому здесь только то, что реально придёт снаружи.
/// </summary>
public class EventArtifact
{
    public Guid Id { get; set; }

    public Guid EventId { get; set; }
    public Event? Event { get; set; }

    public ArtifactKind Kind { get; set; }

    public required string Title { get; set; }

    /// <summary>Подпись под заголовком: «42 мин · доступна».</summary>
    public string? Subtitle { get; set; }

    public string? Url { get; set; }

    public int SortOrder { get; set; }
}
