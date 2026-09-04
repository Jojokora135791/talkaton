namespace Talkaton.Domain.Entities;

/// <summary>
/// Календарь пользователя — «Рабочие встречи», «Личное» и т.п. из блока «Мои календари».
/// </summary>
public class Calendar
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public User? Owner { get; set; }

    public required string Name { get; set; }

    /// <summary>Цвет карточек событий в сетке, HEX вида #4c8dff.</summary>
    public required string Color { get; set; }

    /// <summary>Галочка видимости в левой панели.</summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>Порядок в списке «Мои календари».</summary>
    public int SortOrder { get; set; }
}
