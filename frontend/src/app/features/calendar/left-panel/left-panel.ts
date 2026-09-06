import { ChangeDetectionStrategy, Component, input, output, signal } from '@angular/core';
import { MiniMonth } from '../mini-month/mini-month';
import { Calendar, ParticipantList } from '../../../core/api/models';
import { BirthdayScope } from '../calendar-store';
import { APP_VERSION } from '../../../core/app-version';

/**
 * Левая панель макета: создание встречи, мини-календарь, «Мои календари»
 * с фильтром дней рождения и «Списки участников» с удалением и цветами.
 */
@Component({
  selector: 'app-left-panel',
  imports: [MiniMonth],
  templateUrl: './left-panel.html',
  styleUrl: './left-panel.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LeftPanel {
  readonly calendars = input.required<readonly Calendar[]>();
  readonly participantLists = input.required<readonly ParticipantList[]>();
  readonly selectedDate = input.required<Date>();
  readonly busyDays = input<ReadonlySet<string>>(new Set<string>());
  readonly birthdayScope = input<BirthdayScope>('dept');

  readonly createRequested = output<void>();
  readonly daySelected = output<Date>();
  readonly calendarToggled = output<Calendar>();
  readonly listCreateRequested = output<void>();
  readonly birthdayScopeChanged = output<BirthdayScope>();
  readonly listDeleted = output<ParticipantList>();
  readonly listClicked = output<ParticipantList>();
  readonly delegationRequested = output<void>();

  protected readonly showBdayPopover = signal(false);
  protected readonly popoverTop = signal(0);
  protected readonly popoverLeft = signal(0);
  protected readonly version = APP_VERSION;

  protected isBirthdayCalendar(calendar: Calendar): boolean {
    return calendar.name.toLowerCase().includes('рождения');
  }

  protected togglePopover(event: MouseEvent): void {
    const target = event.currentTarget as HTMLElement;
    const rect = target.getBoundingClientRect();
    this.popoverTop.set(rect.top);
    this.popoverLeft.set(Math.min(rect.right + 8, window.innerWidth - 292));
    this.showBdayPopover.update((v) => !v);
  }

  protected closePopover(): void {
    this.showBdayPopover.set(false);
  }

  protected selectScope(scope: BirthdayScope): void {
    this.birthdayScopeChanged.emit(scope);
    this.closePopover();
  }
}
