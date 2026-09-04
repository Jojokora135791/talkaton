using Talkaton.Domain.Entities;

namespace Talkaton.Domain.Scheduling;

/// <summary>
/// Одно вхождение встречи в сетке. Для разовой встречи оно единственное и совпадает с серией.
/// </summary>
/// <param name="Event">Серия, из которой развёрнуто вхождение.</param>
/// <param name="OccurrenceStartUtc">
/// Начало по расписанию — устойчивый ключ вхождения. Именно он ездит в API как
/// <c>occurrenceStart</c>: после переноса конкретного вхождения ключ не меняется,
/// иначе повторный drag-and-drop создавал бы второе исключение вместо правки первого.
/// </param>
/// <param name="StartUtc">Фактическое начало с учётом исключения.</param>
/// <param name="EndUtc">Фактический конец с учётом исключения.</param>
/// <param name="IsMoved">Вхождение выбито из ритма серии — рисуем пометку в панели.</param>
public readonly record struct EventOccurrence(
    Event Event,
    DateTime OccurrenceStartUtc,
    DateTime StartUtc,
    DateTime EndUtc,
    bool IsMoved);
