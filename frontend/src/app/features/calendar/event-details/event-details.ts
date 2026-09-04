import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ArtifactKind, EditScope, EventDetails, ParticipantStatus } from '../../../core/api/models';
import { joinTalkRoom, talkRoomLabel } from '../../../core/talk/talk-room';
import { formatOccurrenceHeading } from '../../../core/time/date-utils';
import { describeRecurrence } from '../../../core/time/recurrence-text';

/** Только эмодзи, которые везде рисуются цветными: символьные глифы местами дают квадраты. */
const ARTIFACT_ICONS: Record<ArtifactKind, string> = {
  recording: '🎬',
  protocol: '📄',
  board: '🗂',
  tasks: '✅',
};

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

  protected iconOf(kind: ArtifactKind): string {
    return ARTIFACT_ICONS[kind];
  }

  protected reminderLabel(minutes: number): string {
    return minutes === 0 ? 'Не напоминать' : `За ${minutes} мин`;
  }

  protected join(): void {
    joinTalkRoom(this.details().occurrence.talkRoomSlug);
  }

  protected openArtifact(url: string | null): void {
    if (url) {
      window.open(url, '_blank', 'noopener');
    }
  }

  protected onReminderChange(value: string): void {
    this.reminderChanged.emit(Number(value));
  }
}
