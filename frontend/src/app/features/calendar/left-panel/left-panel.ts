import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MiniMonth } from '../mini-month/mini-month';
import { Calendar, ParticipantList } from '../../../core/api/models';
import { APP_VERSION } from '../../../core/app-version';

/**
 * Левая панель макета: создание встречи, мини-календарь, «Мои календари»
 * и «Списки участников».
 *
 * Блок «Дни рождения — кого показывать» намеренно не реализован: на макете он перечёркнут,
 * в плане отнесён к явно невыполняемому скоупу.
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

  readonly createRequested = output<void>();
  readonly daySelected = output<Date>();
  readonly calendarToggled = output<Calendar>();
  readonly listCreateRequested = output<void>();

  protected readonly version = APP_VERSION;
}
