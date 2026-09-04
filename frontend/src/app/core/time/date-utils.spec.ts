import {
  addMonths,
  dayKey,
  formatMinutes,
  formatWeekRange,
  fromDateTimeInputs,
  minutesFromMidnight,
  startOfWeek,
  toTimeInput,
} from './date-utils';

describe('date-utils', () => {
  it('неделя начинается с понедельника', () => {
    // 9 сентября 2026 — среда, как на макете.
    const week = startOfWeek(new Date(2026, 8, 9));

    expect(week.getDay()).toBe(1);
    expect(dayKey(week)).toBe('2026-09-07');
  });

  it('воскресенье относится к уходящей неделе, а не к следующей', () => {
    expect(dayKey(startOfWeek(new Date(2026, 8, 13)))).toBe('2026-09-07');
  });

  it('период недели пишется одним месяцем, когда он один', () => {
    expect(formatWeekRange(new Date(2026, 8, 7))).toBe('7 – 13 сентября 2026');
  });

  it('период недели на стыке месяцев показывает оба', () => {
    expect(formatWeekRange(new Date(2026, 8, 28))).toBe('28 сентября – 4 октября 2026');
  });

  it('сдвиг на месяц не перескакивает через короткий февраль', () => {
    expect(dayKey(addMonths(new Date(2026, 0, 31), 1))).toBe('2026-02-28');
  });

  it('минуты от полуночи считаются в местном времени', () => {
    expect(minutesFromMidnight(new Date(2026, 8, 9, 11, 30))).toBe(11 * 60 + 30);
  });

  it('поля формы превращаются обратно в местное время', () => {
    const parsed = fromDateTimeInputs('2026-09-09', '11:00');

    expect(parsed).not.toBeNull();
    expect(dayKey(parsed!)).toBe('2026-09-09');
    expect(toTimeInput(parsed!)).toBe('11:00');
  });

  it('кривой ввод даты не роняет форму', () => {
    expect(fromDateTimeInputs('', '11:00')).toBeNull();
    expect(fromDateTimeInputs('2026-09-09', 'нет')).toBeNull();
  });

  it('минуты склоняются по-русски', () => {
    expect(formatMinutes(1)).toBe('1 минуту');
    expect(formatMinutes(3)).toBe('3 минуты');
    expect(formatMinutes(5)).toBe('5 минут');
    expect(formatMinutes(11)).toBe('11 минут');
    expect(formatMinutes(21)).toBe('21 минуту');
  });
});
