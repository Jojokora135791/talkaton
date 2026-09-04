import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HealthService } from '../../core/health/health.service';
import { Health } from '../../core/health/health.model';

/** Календари из макета. На этапе 2 приезжают из `GET /api/calendars`. */
interface CalendarChip {
  readonly name: string;
  readonly colorToken: string;
  readonly visible: boolean;
}

@Component({
  selector: 'app-calendar-page',
  templateUrl: './calendar-page.html',
  styleUrl: './calendar-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CalendarPage {
  private readonly healthService = inject(HealthService);

  protected readonly health = signal<Health | null>(null);
  protected readonly healthError = signal<string | null>(null);

  protected readonly calendars: readonly CalendarChip[] = [
    { name: 'Рабочие встречи', colorToken: '--tk-cal-work', visible: true },
    { name: 'Личное', colorToken: '--tk-cal-personal', visible: true },
    { name: 'Дни рождения', colorToken: '--tk-cal-birthday', visible: true },
    { name: 'Задачи и дедлайны', colorToken: '--tk-cal-tasks', visible: false },
  ];

  constructor() {
    this.healthService
      .get()
      .pipe(takeUntilDestroyed())
      .subscribe({
        next: (health) => this.health.set(health),
        error: () => this.healthError.set('API недоступен'),
      });
  }
}
