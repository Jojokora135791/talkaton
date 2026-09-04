/**
 * Человеческие подписи для RRULE и обратно — пресеты редактора.
 * Разбираем ровно то подмножество RFC 5545, которое умеет бэкенд.
 */

const WEEKDAY_CODES: Record<string, string> = {
  MO: 'понедельник',
  TU: 'вторник',
  WE: 'среду',
  TH: 'четверг',
  FR: 'пятницу',
  SA: 'субботу',
  SU: 'воскресенье',
};

export interface RecurrenceOption {
  readonly value: string | null;
  readonly label: string;
}

/** «Повторяется каждую среду» под датой в правой панели. */
export function describeRecurrence(rrule: string | null): string | null {
  if (!rrule) {
    return null;
  }

  const parts = new Map(
    rrule
      .replace(/^RRULE:/i, '')
      .split(';')
      .map((part) => part.split('='))
      .filter((pair): pair is [string, string] => pair.length === 2)
      .map(([name, value]) => [name.toUpperCase(), value.toUpperCase()]),
  );

  const interval = Number(parts.get('INTERVAL') ?? '1');
  const byDay = parts.get('BYDAY');

  switch (parts.get('FREQ')) {
    case 'DAILY':
      return interval === 1 ? 'Повторяется каждый день' : `Повторяется раз в ${interval} дн.`;
    case 'WEEKLY': {
      if (interval > 1) {
        return `Повторяется раз в ${interval} нед.`;
      }
      const days = (byDay ?? '')
        .split(',')
        .map((code) => WEEKDAY_CODES[code])
        .filter(Boolean);

      return days.length > 0 ? `Повторяется каждую ${days.join(', ')}` : 'Повторяется каждую неделю';
    }
    case 'MONTHLY':
      return interval === 1 ? 'Повторяется каждый месяц' : `Повторяется раз в ${interval} мес.`;
    case 'YEARLY':
      return 'Повторяется каждый год';
    default:
      return 'Повторяющаяся встреча';
  }
}

/** Пресеты выпадающего списка «Повторение» в редакторе, привязанные ко дню начала. */
export function recurrenceOptions(start: Date): RecurrenceOption[] {
  const codes = ['SU', 'MO', 'TU', 'WE', 'TH', 'FR', 'SA'];
  const weekday = codes[start.getDay()];

  return [
    { value: null, label: 'Не повторяется' },
    { value: 'FREQ=DAILY', label: 'Каждый день' },
    { value: `FREQ=WEEKLY;BYDAY=${weekday}`, label: 'Каждую неделю' },
    { value: `FREQ=WEEKLY;INTERVAL=2;BYDAY=${weekday}`, label: 'Раз в две недели' },
    { value: 'FREQ=MONTHLY', label: 'Каждый месяц' },
    { value: 'FREQ=YEARLY', label: 'Каждый год' },
  ];
}
