import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CalendarView } from '../calendar-store';

interface ViewOption {
  readonly value: CalendarView;
  readonly label: string;
  readonly shortcut: string;
}

/** Верхняя панель сетки: «Сегодня», стрелки, период, поиск и переключатель вида. */
@Component({
  selector: 'app-calendar-toolbar',
  imports: [FormsModule],
  templateUrl: './calendar-toolbar.html',
  styleUrl: './calendar-toolbar.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CalendarToolbar {
  readonly title = input.required<string>();
  readonly view = input.required<CalendarView>();
  readonly search = input('');
  readonly loading = input(false);

  readonly todayRequested = output<void>();
  readonly stepped = output<-1 | 1>();
  readonly viewChanged = output<CalendarView>();
  readonly searchChanged = output<string>();

  protected readonly views: readonly ViewOption[] = [
    { value: 'day', label: 'День', shortcut: 'D' },
    { value: 'week', label: 'Неделя', shortcut: 'W' },
    { value: 'month', label: 'Месяц', shortcut: 'M' },
    { value: 'year', label: 'Год', shortcut: 'Y' },
  ];
}
