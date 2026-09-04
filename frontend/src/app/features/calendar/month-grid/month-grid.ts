import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { Occurrence } from '../../../core/api/models';
import { OccurrenceKey, isSameOccurrence } from '../calendar-store';
import {
  addDays,
  dayKey,
  formatTime,
  formatWeekdayShort,
  isToday,
  startOfDay,
  startOfWeek,
} from '../../../core/time/date-utils';

const DAYS = 7;

/** Больше в ячейку не влезает — остальное сворачиваем в «ещё N». */
const MAX_CHIPS = 3;

interface MonthDay {
  date: Date;
  key: string;
  number: number;
  inMonth: boolean;
  today: boolean;
  chips: MonthChip[];
  hiddenCount: number;
}

interface MonthChip {
  occurrence: Occurrence;
  label: string;
  selected: boolean;
}

/** Месячная сетка: шесть недель, в ячейке — до трёх встреч и счётчик остальных. */
@Component({
  selector: 'app-month-grid',
  templateUrl: './month-grid.html',
  styleUrl: './month-grid.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MonthGrid {
  readonly days = input.required<readonly Date[]>();
  readonly occurrences = input.required<readonly Occurrence[]>();
  readonly month = input.required<Date>();
  readonly selected = input<OccurrenceKey | null>(null);

  readonly daySelected = output<Date>();
  readonly occurrenceSelected = output<Occurrence>();

  protected readonly weekdays = computed(() => {
    const from = startOfWeek(new Date());
    return Array.from({ length: DAYS }, (_, index) => formatWeekdayShort(addDays(from, index)));
  });

  protected readonly weeks = computed<MonthDay[][]>(() => {
    const days = this.days();
    const month = this.month().getMonth();
    const selected = this.selected();
    const byDay = groupByDay(this.occurrences());

    const cells = days.map<MonthDay>((date) => {
      const all = byDay.get(dayKey(date)) ?? [];

      return {
        date,
        key: dayKey(date),
        number: date.getDate(),
        inMonth: date.getMonth() === month,
        today: isToday(date),
        chips: all.slice(0, MAX_CHIPS).map((occurrence) => ({
          occurrence,
          label: occurrence.isAllDay
            ? occurrence.title
            : `${formatTime(new Date(occurrence.startUtc))} ${occurrence.title}`,
          selected: isSameOccurrence(occurrence, selected),
        })),
        hiddenCount: Math.max(0, all.length - MAX_CHIPS),
      };
    });

    return Array.from({ length: Math.ceil(cells.length / DAYS) }, (_, week) =>
      cells.slice(week * DAYS, week * DAYS + DAYS),
    );
  });
}

/** Раскладываем вхождения по дням: длинная встреча попадает в каждый задетый день. */
function groupByDay(occurrences: readonly Occurrence[]): Map<string, Occurrence[]> {
  const byDay = new Map<string, Occurrence[]>();

  for (const occurrence of occurrences) {
    const start = new Date(occurrence.startUtc);
    const end = new Date(occurrence.endUtc);

    for (let day = startOfDay(start); day < end; day = addDays(day, 1)) {
      const key = dayKey(day);
      const bucket = byDay.get(key);
      if (bucket) {
        bucket.push(occurrence);
      } else {
        byDay.set(key, [occurrence]);
      }
    }
  }

  for (const bucket of byDay.values()) {
    bucket.sort((left, right) => left.startUtc.localeCompare(right.startUtc));
  }

  return byDay;
}
