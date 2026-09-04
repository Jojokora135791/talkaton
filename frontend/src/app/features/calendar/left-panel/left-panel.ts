import { ChangeDetectionStrategy, Component, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MiniMonth } from '../mini-month/mini-month';
import { Calendar, ParticipantList } from '../../../core/api/models';

/**
 * Левая панель макета: создание встречи, мини-календарь, «Мои календари»
 * и «Списки участников».
 *
 * Блок «Дни рождения — кого показывать» намеренно не реализован: на макете он перечёркнут,
 * в плане отнесён к явно невыполняемому скоупу.
 */
@Component({
  selector: 'app-left-panel',
  imports: [FormsModule, MiniMonth],
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
  readonly listCreated = output<string>();

  protected readonly newListName = signal('');
  protected readonly addingList = signal(false);

  protected startAddingList(): void {
    this.addingList.set(true);
  }

  protected submitList(): void {
    const name = this.newListName().trim();
    if (name.length > 0) {
      this.listCreated.emit(name);
    }

    this.cancelList();
  }

  protected cancelList(): void {
    this.newListName.set('');
    this.addingList.set(false);
  }
}
