import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { ReminderService } from '../reminder.service';
import { joinTalkRoom } from '../../../core/talk/talk-room';
import { formatMinutes, formatTime } from '../../../core/time/date-utils';

/**
 * Тост «до начала N минут» поверх интерфейса — ключевая фича из плана 2.4.
 * Живёт в оболочке приложения, а не в календаре: напомнить нужно и тогда,
 * когда человек ушёл в другой раздел.
 */
@Component({
  selector: 'app-reminder-toast',
  templateUrl: './reminder-toast.html',
  styleUrl: './reminder-toast.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReminderToast {
  private readonly reminders = inject(ReminderService);

  protected readonly pending = this.reminders.pending;

  protected readonly headline = computed(() => {
    const pending = this.pending();
    if (!pending) {
      return '';
    }

    return pending.inProgress
      ? `Идёт уже ${formatMinutes(pending.minutes)} — вас ещё нет`
      : `До начала ${formatMinutes(pending.minutes)}`;
  });

  protected readonly timeRange = computed(() => {
    const pending = this.pending();
    if (!pending) {
      return '';
    }

    const start = new Date(pending.occurrence.startUtc);
    const end = new Date(pending.occurrence.endUtc);
    return `${formatTime(start)} – ${formatTime(end)}`;
  });

  protected join(): void {
    const pending = this.pending();
    if (!pending) {
      return;
    }

    joinTalkRoom(pending.occurrence.talkRoomSlug);
    this.reminders.dismiss(pending);
  }

  protected dismiss(): void {
    const pending = this.pending();
    if (pending) {
      this.reminders.dismiss(pending);
    }
  }
}
