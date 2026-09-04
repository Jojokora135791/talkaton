import { MONTH_GRID_WEEKS, rangeFor } from './calendar-store';
import { dayKey } from '../../core/time/date-utils';

describe('rangeFor', () => {
  // 9 сентября 2026 — среда с макета.
  const anchor = new Date(2026, 8, 9);

  it('день просит ровно сутки', () => {
    const { from, to } = rangeFor('day', anchor);

    expect(dayKey(from)).toBe('2026-09-09');
    expect(dayKey(to)).toBe('2026-09-10');
  });

  it('неделя начинается с понедельника и длится семь дней', () => {
    const { from, to } = rangeFor('week', anchor);

    expect(dayKey(from)).toBe('2026-09-07');
    expect(dayKey(to)).toBe('2026-09-14');
  });

  it('месяц берёт шесть полных недель, чтобы сетка не прыгала', () => {
    const { from, to } = rangeFor('month', anchor);

    // 1 сентября 2026 — вторник, поэтому сетка начинается с 31 августа.
    expect(dayKey(from)).toBe('2026-08-31');
    expect((to.getTime() - from.getTime()) / 86_400_000).toBe(MONTH_GRID_WEEKS * 7);
  });

  it('год берётся целиком', () => {
    const { from, to } = rangeFor('year', anchor);

    expect(dayKey(from)).toBe('2026-01-01');
    expect(dayKey(to)).toBe('2027-01-01');
  });
});
