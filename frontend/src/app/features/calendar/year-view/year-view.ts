import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import {
  addDays,
  dayKey,
  formatMonth,
  formatWeekdayShort,
  isToday,
  startOfWeek,
} from '../../../core/time/date-utils';

const DAYS = 7;
const WEEKS = 6;

interface YearDay {
  date: Date;
  key: string;
  number: number;
  inMonth: boolean;
  today: boolean;
  busy: boolean;
}

interface YearMonth {
  title: string;
  index: number;
  weeks: YearDay[][];
}

/**
 * Годовой обзор: двенадцать месяцев с отметками занятых дней.
 * Нужен, чтобы увидеть плотность года и провалиться в нужный день одним щелчком.
 */
@Component({
  selector: 'app-year-view',
  templateUrl: './year-view.html',
  styleUrl: './year-view.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class YearView {
  readonly year = input.required<number>();
  readonly busyDays = input.required<ReadonlySet<string>>();

  readonly daySelected = output<Date>();

  protected readonly weekdays = computed(() => {
    const from = startOfWeek(new Date());
    return Array.from({ length: DAYS }, (_, index) => formatWeekdayShort(addDays(from, index))[0]);
  });

  protected readonly months = computed<YearMonth[]>(() => {
    const year = this.year();
    const busy = this.busyDays();

    return Array.from({ length: 12 }, (_, month) => {
      const first = startOfWeek(new Date(year, month, 1));

      return {
        index: month,
        title: formatMonth(new Date(year, month, 1)),
        weeks: Array.from({ length: WEEKS }, (_, week) =>
          Array.from({ length: DAYS }, (_, day) => {
            const date = addDays(first, week * DAYS + day);
            const key = dayKey(date);

            return {
              date,
              key,
              number: date.getDate(),
              inMonth: date.getMonth() === month,
              today: isToday(date),
              busy: busy.has(key),
            };
          }),
        ),
      };
    });
  });
}
