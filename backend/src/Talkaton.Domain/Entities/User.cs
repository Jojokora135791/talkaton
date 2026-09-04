namespace Talkaton.Domain.Entities;

/// <summary>
/// Пользователь Толка. На этапе 1 заводится сидом, на этапе 3 приходит из ITalkGateway.
/// </summary>
public class User
{
    public Guid Id { get; set; }
    public required string DisplayName { get; set; }
    public required string Email { get; set; }

    /// <summary>Таймзона IANA, например "Asia/Yekaterinburg". Время событий храним в UTC.</summary>
    public string TimeZoneId { get; set; } = "UTC";

    public ICollection<Calendar> Calendars { get; set; } = new List<Calendar>();
}
