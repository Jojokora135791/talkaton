import { describeRecurrence, recurrenceOptions } from './recurrence-text';

describe('recurrence-text', () => {
  it('описывает правило встречи с макета', () => {
    expect(describeRecurrence('FREQ=WEEKLY;BYDAY=WE')).toBe('Повторяется каждую среду');
  });

  it('понимает правило с префиксом RRULE и в нижнем регистре', () => {
    expect(describeRecurrence('rrule:freq=daily')).toBe('Повторяется каждый день');
  });

  it('разовая встреча не описывается вовсе', () => {
    expect(describeRecurrence(null)).toBeNull();
  });

  it('интервал показывается отдельно от дней недели', () => {
    expect(describeRecurrence('FREQ=WEEKLY;INTERVAL=2;BYDAY=WE')).toBe('Повторяется раз в 2 нед.');
  });

  it('незнакомое правило не остаётся без подписи', () => {
    expect(describeRecurrence('FREQ=HOURLY')).toBe('Повторяющаяся встреча');
  });

  it('пресет «каждую неделю» привязан ко дню начала', () => {
    // 9 сентября 2026 — среда.
    const options = recurrenceOptions(new Date(2026, 8, 9));

    expect(options[0].value).toBeNull();
    expect(options[2].value).toBe('FREQ=WEEKLY;BYDAY=WE');
    expect(options[3].value).toBe('FREQ=WEEKLY;INTERVAL=2;BYDAY=WE');
  });
});
