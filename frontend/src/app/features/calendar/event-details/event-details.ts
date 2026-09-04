import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { EditScope, EventDetails, ParticipantStatus } from '../../../core/api/models';
import { joinTalkRoom, talkRoomLabel } from '../../../core/talk/talk-room';
import { formatOccurrenceHeading } from '../../../core/time/date-utils';
import { describeRecurrence } from '../../../core/time/recurrence-text';

const STATUS_LABELS: Record<ParticipantStatus, string> = {
  accepted: '✓ идёт',
  tentative: '? возможно',
  declined: '✕ не идёт',
};

/** Варианты из плана 2.4: за 5, 10 или 15 минут, либо не напоминать. */
const REMINDER_CHOICES = [0, 5, 10, 15, 30];

/**
 * Правая панель встречи с макета: время, ссылка в Толк, артефакты и участники.
 */
@Component({
  selector: 'app-event-details',
  imports: [FormsModule],
  templateUrl: './event-details.html',
  styleUrl: './event-details.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EventDetailsPanel {
  readonly details = input.required<EventDetails>();

  readonly closed = output<void>();
  readonly editRequested = output<void>();
  readonly deleteRequested = output<EditScope>();
  readonly rsvpChanged = output<ParticipantStatus>();
  readonly reminderChanged = output<number>();

  protected readonly statusLabels = STATUS_LABELS;
  protected readonly reminderChoices = REMINDER_CHOICES;

  protected readonly heading = computed(() => {
    const occurrence = this.details().occurrence;
    return formatOccurrenceHeading(
      new Date(occurrence.startUtc),
      new Date(occurrence.endUtc),
      occurrence.isAllDay,
    );
  });

  protected readonly recurrenceText = computed(() =>
    describeRecurrence(this.details().occurrence.recurrenceRule),
  );

  protected readonly roomLabel = computed(() => talkRoomLabel(this.details().occurrence.talkRoomSlug));

  protected readonly rsvpOptions: readonly ParticipantStatus[] = ['accepted', 'tentative', 'declined'];

  protected reminderLabel(minutes: number): string {
    return minutes === 0 ? 'Не напоминать' : `За ${minutes} мин`;
  }

  protected join(): void {
    joinTalkRoom(this.details().occurrence.talkRoomSlug);
  }

  protected openArtifact(url: string | null): void {
    const safeUrl = this.artifactUrl(url);
    if (safeUrl) {
      window.open(safeUrl, '_blank', 'noopener,noreferrer');
    }
  }

  protected artifactAvailable(url: string | null): boolean {
    return this.artifactUrl(url) !== null;
  }

  protected initials(name: string): string {
    return name
      .split(' ')
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part.charAt(0).toUpperCase())
      .join('');
  }

  protected onReminderChange(value: string): void {
    this.reminderChanged.emit(Number(value));
  }

  /** Артефакт приходит из API: открываем только обычные web-ссылки, не javascript:/data:. */
  private artifactUrl(value: string | null): string | null {
    if (!value) {
      return null;
    }

    try {
      const url = new URL(value, window.location.origin);
      return url.protocol === 'http:' || url.protocol === 'https:' ? url.toString() : null;
    } catch {
      return null;
    }
  }
}
