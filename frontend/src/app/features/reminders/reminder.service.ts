import { DestroyRef, Injectable, computed, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TalkatonApi } from '../../core/api/talkaton-api';
import { Occurrence } from '../../core/api/models';
import { addMinutes, toIso } from '../../core/time/date-utils';

/** Как часто пересчитываем «сколько осталось». Секунды в тосте не показываем. */
const TICK_MS = 15_000;

/** Как часто перезапрашиваем ближайшие встречи: их мог создать кто-то другой. */
const RELOAD_MS = 2 * 60_000;

/** Горизонт, в котором вообще имеет смысл напоминать. */
const HORIZON_MINUTES = 12 * 60;

/** Сколько минут после начала показываем «встреча уже идёт», прежде чем замолчать. */
const IN_PROGRESS_GRACE_MINUTES = 10;

export interface PendingReminder {
  occurrence: Occurrence;
  /** До начала (для будущей) или с начала (для идущей). */
  minutes: number;
  /** Встреча уже идёт, а вы ещё не в ней — отдельный случай из плана 2.4. */
  inProgress: boolean;
}

/**
 * Клиентский планировщик напоминаний (план 2.4). Считает от списка ближайших встреч,
 * а не от таймеров на каждую: пересчёт по тику переживает и сон ноутбука,
 * и правку встречи в соседней вкладке.
 */
@Injectable({ providedIn: 'root' })
export class ReminderService {
  private readonly api = inject(TalkatonApi);
  private readonly destroyRef = inject(DestroyRef);

  private readonly upcoming = signal<Occurrence[]>([]);
  private readonly now = signal(new Date());
  private readonly dismissed = signal<ReadonlySet<string>>(new Set());
  private readonly notified = new Set<string>();

  private tickTimer?: ReturnType<typeof setInterval>;
  private reloadTimer?: ReturnType<typeof setInterval>;

  readonly permission = signal<NotificationPermission>(currentPermission());

  readonly pending = computed<PendingReminder | null>(() => {
    const now = this.now().getTime();
    const dismissed = this.dismissed();

    for (const occurrence of this.upcoming()) {
      if (occurrence.isAllDay || occurrence.myStatus === 'declined' || dismissed.has(keyOf(occurrence))) {
        continue;
      }

      const start = Date.parse(occurrence.startUtc);
      const end = Date.parse(occurrence.endUtc);
      const leadMinutes = occurrence.reminderMinutesBefore ?? 0;

      if (leadMinutes > 0 && now >= start - leadMinutes * 60_000 && now < start) {
        return {
          occurrence,
          minutes: Math.max(1, Math.round((start - now) / 60_000)),
          inProgress: false,
        };
      }

      // Встреча идёт, а вы ещё не в ней. Дальше грейс-периода не напоминаем:
      // на двухчасовом созвоне тост превратился бы в фон.
      const startedMinutes = Math.floor((now - start) / 60_000);
      if (now >= start && now < end && startedMinutes <= IN_PROGRESS_GRACE_MINUTES && occurrence.talkRoomSlug) {
        return { occurrence, minutes: startedMinutes, inProgress: true };
      }
    }

    return null;
  });

  constructor() {
    // Как только напоминание всплыло — дублируем его системным уведомлением,
    // чтобы календарь работал и в фоновой вкладке.
    effect(() => {
      const pending = this.pending();
      if (pending) {
        this.notifyOnce(pending);
      }
    });

    this.destroyRef.onDestroy(() => this.stop());
  }

  start(): void {
    if (this.tickTimer) {
      return;
    }

    this.reload();
    this.tickTimer = setInterval(() => this.now.set(new Date()), TICK_MS);
    this.reloadTimer = setInterval(() => this.reload(), RELOAD_MS);
  }

  stop(): void {
    clearInterval(this.tickTimer);
    clearInterval(this.reloadTimer);
    this.tickTimer = undefined;
    this.reloadTimer = undefined;
    this.upcoming.set([]);
  }

  /** Перечитывает ближайшие встречи — после правки в календаре, чтобы тост не отстал. */
  reload(): void {
    const from = addMinutes(new Date(), -IN_PROGRESS_GRACE_MINUTES);

    this.api
      .events(toIso(from), toIso(addMinutes(from, HORIZON_MINUTES)))
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (occurrences) => {
          this.upcoming.set(occurrences);
          this.now.set(new Date());
        },
        // Тихо: отвалившийся бэкенд уже виден по индикатору в подвале сетки.
        error: () => undefined,
      });
  }

  dismiss(reminder: PendingReminder): void {
    // Новый Set, а не мутация: иначе сигнал не заметит изменения и тост не закроется.
    this.dismissed.update((all) => new Set(all).add(keyOf(reminder.occurrence)));
  }

  /** Разрешение спрашиваем по кнопке: без жеста человека браузеры его и не дадут. */
  requestPermission(): void {
    if (typeof Notification === 'undefined') {
      return;
    }

    void Notification.requestPermission().then((result) => this.permission.set(result));
  }

  private notifyOnce(pending: PendingReminder): void {
    const key = keyOf(pending.occurrence);
    if (this.notified.has(key) || typeof Notification === 'undefined' || Notification.permission !== 'granted') {
      return;
    }

    this.notified.add(key);
    new Notification(pending.occurrence.title, {
      body: pending.inProgress ? 'Встреча уже идёт' : `Начнётся через ${pending.minutes} мин`,
      tag: key,
    });
  }
}

function keyOf(occurrence: Occurrence): string {
  return `${occurrence.eventId}@${occurrence.occurrenceStartUtc}`;
}

function currentPermission(): NotificationPermission {
  return typeof Notification === 'undefined' ? 'denied' : Notification.permission;
}
