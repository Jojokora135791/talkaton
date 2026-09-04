namespace Talkaton.Domain.Entities;

public enum ExternalCalendarProvider
{
    Google = 0,
    Ics = 1,
}

/// <summary>
/// Подключённый внешний календарь. На этапе 2 сущность только заводится в схеме:
/// сама синхронизация — этап 3.3. Дешевле добавить таблицу сейчас, чем мигрировать
/// боевую базу на уже поднятом стенде.
/// </summary>
public class ExternalAccount
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public ExternalCalendarProvider Provider { get; set; }

    /// <summary>Адрес аккаунта Google или URL ICS-фида.</summary>
    public required string AccountName { get; set; }

    /// <summary>syncToken Google для инкрементальных обновлений.</summary>
    public string? SyncToken { get; set; }

    public DateTime? LastSyncedUtc { get; set; }
}
