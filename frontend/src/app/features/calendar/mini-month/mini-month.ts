import { ChangeDetectionStrategy, Component, computed, input, linkedSignal, output } from '@angular/core';
import {
  addDays,
  addMonths,
  dayKey,
  formatMonthYear,
  formatWeekdayShort,
  isSameDay,
  isToday,
  startOfMonth,
  startOfWeek,
} from '../../../core/time/date-utils';

const WEEKS = 6;
const DAYS = 7;

interface MiniDay {
  date: Date;
  key: string;
  label: number;
  inMonth: boolean;
  today: boolean;
}

/**
 * Мини-календарь месяца из левой панели: навигация по месяцам и быстрый переход к дню.
 */
@Component({
  selector: 'app-mini-month',
  templateUrl: './mini-month.html',
  styleUrl: './mini-month.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MiniMonth {
  /** Выбранный в календаре день — он же подсвечен и задаёт показываемый месяц. */
  readonly selected = input.required<Date>();

  /**
   * Дни, в которых есть встречи. Приходят из уже загруженного периода, поэтому
   * точки видны там, где сетка сейчас смотрит, — отдельный запрос ради них не делаем.
   */
  readonly busyDays = input<ReadonlySet<string>>(new Set<string>());

  readonly daySelected = output<Date>();

  /** Пролистанный руками месяц держится, пока в календаре не выбрали день из другого месяца. */
  protected readonly month = linkedSignal<number, Date>({
    source: () => startOfMonth(this.selected()).getTime(),
    computation: (source) => new Date(source),
  });

  protected readonly title = computed(() => formatMonthYear(this.month()));

  protected readonly weekdays = computed(() => {
    const from = startOfWeek(new Date());
    return Array.from({ length: DAYS }, (_, index) => formatWeekdayShort(addDays(from, index)).toLowerCase());
  });

  protected readonly weeks = computed<MiniDay[][]>(() => {
    const month = this.month();
    const first = startOfWeek(startOfMonth(month));

    return Array.from({ length: WEEKS }, (_, week) =>
      Array.from({ length: DAYS }, (_, day) => {
        const date = addDays(first, week * DAYS + day);
        return {
          date,
          key: dayKey(date),
          label: date.getDate(),
          inMonth: date.getMonth() === month.getMonth(),
          today: isToday(date),
        };
      }),
    );
  });

  protected isSelected(date: Date): boolean {
    return isSameDay(date, this.selected());
  }

  protected step(direction: -1 | 1): void {
    this.month.set(addMonths(this.month(), direction));
  }
}
