/**
 * Работа с датами в местном времени браузера. Бэкенд говорит только в UTC,
 * поэтому граница простая: сюда приходят Date, наружу уходят ISO-строки.
 */

export const MINUTES_IN_DAY = 24 * 60;
export const DAYS_IN_WEEK = 7;

export function startOfDay(date: Date): Date {
  const copy = new Date(date);
  copy.setHours(0, 0, 0, 0);
  return copy;
}

export function addDays(date: Date, days: number): Date {
  const copy = new Date(date);
  copy.setDate(copy.getDate() + days);
  return copy;
}

export function addMinutes(date: Date, minutes: number): Date {
  return new Date(date.getTime() + minutes * 60_000);
}

export function addMonths(date: Date, months: number): Date {
  const copy = new Date(date);
  // Сначала 1-е число: иначе 31 марта минус месяц превращается в 3 марта.
  copy.setDate(1);
  copy.setMonth(copy.getMonth() + months);
  copy.setDate(Math.min(date.getDate(), daysInMonth(copy)));
  return copy;
}

export function daysInMonth(date: Date): number {
  return new Date(date.getFullYear(), date.getMonth() + 1, 0).getDate();
}

/** Неделя начинается с понедельника — как на макете. */
export function startOfWeek(date: Date): Date {
  const copy = startOfDay(date);
  return addDays(copy, -((copy.getDay() + 6) % 7));
}

export function startOfMonth(date: Date): Date {
  const copy = startOfDay(date);
  copy.setDate(1);
  return copy;
}

export function startOfYear(date: Date): Date {
  return new Date(date.getFullYear(), 0, 1);
}

export function isSameDay(left: Date, right: Date): boolean {
  return (
    left.getFullYear() === right.getFullYear() &&
    left.getMonth() === right.getMonth() &&
    left.getDate() === right.getDate()
  );
}

/** Ключ дня в местном времени: `2026-09-09`. Годится и для Map, и для сравнения. */
export function dayKey(date: Date): string {
  const month = `${date.getMonth() + 1}`.padStart(2, '0');
  const day = `${date.getDate()}`.padStart(2, '0');
  return `${date.getFullYear()}-${month}-${day}`;
}

export function isToday(date: Date): boolean {
  return isSameDay(date, new Date());
}

/** Минут от полуночи — по ним карточка встречи находит своё место в сетке. */
export function minutesFromMidnight(date: Date): number {
  return date.getHours() * 60 + date.getMinutes();
}

export function minutesBetween(from: Date, to: Date): number {
  return Math.round((to.getTime() - from.getTime()) / 60_000);
}

export function clamp(value: number, min: number, max: number): number {
  return Math.min(Math.max(value, min), max);
}

export function toIso(date: Date): string {
  return date.toISOString();
}

/** Значение для `<input type="date">` — местная дата, без сдвига в UTC. */
export function toDateInput(date: Date): string {
  return dayKey(date);
}

/** Значение для `<input type="time">`. */
export function toTimeInput(date: Date): string {
  const hours = `${date.getHours()}`.padStart(2, '0');
  const minutes = `${date.getMinutes()}`.padStart(2, '0');
  return `${hours}:${minutes}`;
}

/** Обратно из полей формы в местное время. Кривой ввод не роняет форму — возвращаем null. */
export function fromDateTimeInputs(dateValue: string, timeValue: string): Date | null {
  const [year, month, day] = dateValue.split('-').map(Number);
  const [hours, minutes] = timeValue.split(':').map(Number);

  if ([year, month, day, hours, minutes].some((part) => !Number.isFinite(part))) {
    return null;
  }

  return new Date(year, month - 1, day, hours, minutes, 0, 0);
}

const timeFormat = new Intl.DateTimeFormat('ru-RU', { hour: '2-digit', minute: '2-digit' });
const dayMonthFormat = new Intl.DateTimeFormat('ru-RU', { day: 'numeric', month: 'long' });
const weekdayShortFormat = new Intl.DateTimeFormat('ru-RU', { weekday: 'short' });
const monthYearFormat = new Intl.DateTimeFormat('ru-RU', { month: 'long', year: 'numeric' });
const monthFormat = new Intl.DateTimeFormat('ru-RU', { month: 'long' });
const fullDateFormat = new Intl.DateTimeFormat('ru-RU', {
  weekday: 'long',
  day: 'numeric',
  month: 'long',
});

export function formatTime(date: Date): string {
  return timeFormat.format(date);
}

export function formatDayMonth(date: Date): string {
  return dayMonthFormat.format(date);
}

export function formatWeekdayShort(date: Date): string {
  return weekdayShortFormat.format(date).replace('.', '').toUpperCase();
}

export function formatMonthYear(date: Date): string {
  return capitalize(monthYearFormat.format(date).replace(' г.', ''));
}

export function formatMonth(date: Date): string {
  return capitalize(monthFormat.format(date));
}

export function formatFullDate(date: Date): string {
  return capitalize(fullDateFormat.format(date));
}

/** «Среда, 9 сентября · 11:00 – 11:45» — шапка правой панели. */
export function formatOccurrenceHeading(start: Date, end: Date, isAllDay: boolean): string {
  const day = formatFullDate(start);
  return isAllDay ? `${day} · весь день` : `${day} · ${formatTime(start)} – ${formatTime(end)}`;
}

/** «7 – 13 сентября 2026» в шапке сетки; месяцы разные — «28 сентября – 4 октября 2026». */
export function formatWeekRange(weekStart: Date): string {
  const weekEnd = addDays(weekStart, DAYS_IN_WEEK - 1);
  const year = weekEnd.getFullYear();

  if (weekStart.getMonth() === weekEnd.getMonth()) {
    return `${weekStart.getDate()} – ${formatDayMonth(weekEnd)} ${year}`;
  }

  return `${formatDayMonth(weekStart)} – ${formatDayMonth(weekEnd)} ${year}`;
}

/** «через 7 минут» / «идёт 3 минуты» — подписи в тосте напоминания. */
export function formatMinutes(minutes: number): string {
  const value = Math.abs(minutes);
  const lastTwo = value % 100;
  const last = value % 10;

  if (lastTwo >= 11 && lastTwo <= 14) {
    return `${value} минут`;
  }
  if (last === 1) {
    return `${value} минуту`;
  }
  if (last >= 2 && last <= 4) {
    return `${value} минуты`;
  }
  return `${value} минут`;
}

function capitalize(text: string): string {
  return text.charAt(0).toUpperCase() + text.slice(1);
}
