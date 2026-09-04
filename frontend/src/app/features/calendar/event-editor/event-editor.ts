import { ChangeDetectionStrategy, Component, computed, input, linkedSignal, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Calendar, ParticipantList, User } from '../../../core/api/models';
import {
  addMinutes,
  fromDateTimeInputs,
  toDateInput,
  toIso,
  toTimeInput,
} from '../../../core/time/date-utils';
import { recurrenceOptions } from '../../../core/time/recurrence-text';

/** Что редактор отдаёт наружу. Страница сама решает, создать встречу или обновить. */
export interface EventDraft {
  calendarId: string;
  title: string;
  description: string | null;
  startUtc: string;
  endUtc: string;
  isAllDay: boolean;
  recurrenceRule: string | null;
  talkRoomSlug: string | null;
  participantIds: string[];
  reminderMinutesBefore: number;
}

/** Начальное состояние формы: либо пустая встреча на выбранный слот, либо существующая. */
export interface EventEditorSeed {
  mode: 'create' | 'edit';
  eventId: string | null;
  calendarId: string;
  title: string;
  description: string;
  start: Date;
  end: Date;
  isAllDay: boolean;
  recurrenceRule: string | null;
  talkRoomSlug: string;
  participantIds: string[];
  reminderMinutesBefore: number;
}

const REMINDER_CHOICES = [0, 5, 10, 15, 30];

/** Диалог «Создать встречу» и правки существующей. */
@Component({
  selector: 'app-event-editor',
  imports: [FormsModule],
  templateUrl: './event-editor.html',
  styleUrl: './event-editor.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EventEditor {
  readonly seed = input.required<EventEditorSeed>();
  readonly calendars = input.required<readonly Calendar[]>();
  readonly people = input.required<readonly User[]>();
  readonly participantLists = input.required<readonly ParticipantList[]>();

  readonly saved = output<EventDraft>();
  readonly cancelled = output<void>();

  protected readonly reminderChoices = REMINDER_CHOICES;

  protected readonly title = linkedSignal(() => this.seed().title);
  protected readonly calendarId = linkedSignal(() => this.seed().calendarId);
  protected readonly description = linkedSignal(() => this.seed().description);
  protected readonly dateValue = linkedSignal(() => toDateInput(this.seed().start));
  protected readonly startTime = linkedSignal(() => toTimeInput(this.seed().start));
  protected readonly endTime = linkedSignal(() => toTimeInput(this.seed().end));
  protected readonly isAllDay = linkedSignal(() => this.seed().isAllDay);
  protected readonly recurrence = linkedSignal(() => this.seed().recurrenceRule);
  protected readonly talkRoomSlug = linkedSignal(() => this.seed().talkRoomSlug);
  protected readonly reminder = linkedSignal(() => this.seed().reminderMinutesBefore);
  protected readonly participants = linkedSignal(() => new Set(this.seed().participantIds));

  protected readonly validationError = signal<string | null>(null);

  protected readonly heading = computed(() =>
    this.seed().mode === 'create' ? 'Новая встреча' : 'Изменить встречу',
  );

  /** Пресеты повторения зависят от дня недели: «каждую среду» считается от даты начала. */
  protected readonly recurrenceChoices = computed(() =>
    recurrenceOptions(fromDateTimeInputs(this.dateValue(), this.startTime()) ?? this.seed().start),
  );

  protected reminderLabel(minutes: number): string {
    return minutes === 0 ? 'Не напоминать' : `За ${minutes} мин`;
  }

  protected isChosen(userId: string): boolean {
    return this.participants().has(userId);
  }

  protected toggleParticipant(userId: string): void {
    this.participants.update((chosen) => {
      const next = new Set(chosen);
      if (!next.delete(userId)) {
        next.add(userId);
      }

      return next;
    });
  }

  /** Кнопка списка добавляет всех разом — на макете списки для этого и заведены. */
  protected addList(list: ParticipantList): void {
    this.participants.update((chosen) => {
      const next = new Set(chosen);
      for (const member of list.members) {
        next.add(member.id);
      }

      return next;
    });
  }

  protected submit(): void {
    const title = this.title().trim();
    if (title.length === 0) {
      this.validationError.set('Укажите тему встречи');
      return;
    }

    const allDay = this.isAllDay();
    const start = allDay
      ? fromDateTimeInputs(this.dateValue(), '00:00')
      : fromDateTimeInputs(this.dateValue(), this.startTime());
    const end = allDay
      ? fromDateTimeInputs(this.dateValue(), '23:59')
      : fromDateTimeInputs(this.dateValue(), this.endTime());

    if (!start || !end) {
      this.validationError.set('Проверьте дату и время');
      return;
    }

    // Конец раньше начала обычно значит встречу через полночь — переносим на следующий день.
    const finish = end <= start ? addMinutes(end, 24 * 60) : end;

    this.validationError.set(null);
    this.saved.emit({
      calendarId: this.calendarId(),
      title,
      description: this.description().trim() || null,
      startUtc: toIso(start),
      endUtc: toIso(finish),
      isAllDay: allDay,
      recurrenceRule: this.recurrence(),
      talkRoomSlug: this.talkRoomSlug().trim() || null,
      participantIds: [...this.participants()],
      reminderMinutesBefore: this.reminder(),
    });
  }
}
