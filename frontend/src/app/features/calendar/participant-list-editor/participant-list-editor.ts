import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { User } from '../../../core/api/models';

export interface ParticipantListDraft {
  readonly name: string;
  readonly memberIds: string[];
}

@Component({
  selector: 'app-participant-list-editor',
  imports: [FormsModule],
  templateUrl: './participant-list-editor.html',
  styleUrl: './participant-list-editor.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ParticipantListEditor {
  readonly people = input.required<readonly User[]>();
  readonly saving = input(false);
  readonly serverError = input<string | null>(null);

  readonly saved = output<ParticipantListDraft>();
  readonly cancelled = output<void>();

  protected readonly name = signal('');
  protected readonly query = signal('');
  protected readonly chosen = signal<ReadonlySet<string>>(new Set<string>());
  protected readonly validationError = signal<string | null>(null);

  protected readonly visiblePeople = computed(() => {
    const query = this.query().trim().toLocaleLowerCase('ru');
    return query.length === 0
      ? this.people()
      : this.people().filter((person) => person.displayName.toLocaleLowerCase('ru').includes(query));
  });

  protected isChosen(userId: string): boolean {
    return this.chosen().has(userId);
  }

  protected toggle(userId: string): void {
    this.chosen.update((current) => {
      const next = new Set(current);
      if (!next.delete(userId)) {
        next.add(userId);
      }
      return next;
    });
    this.validationError.set(null);
  }

  protected initials(name: string): string {
    return name
      .split(' ')
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part.charAt(0).toUpperCase())
      .join('');
  }

  protected submit(): void {
    const name = this.name().trim();
    if (name.length === 0) {
      this.validationError.set('Укажите название списка');
      return;
    }
    if (this.chosen().size === 0) {
      this.validationError.set('Выберите хотя бы одного участника');
      return;
    }

    this.validationError.set(null);
    this.saved.emit({ name, memberIds: [...this.chosen()] });
  }
}
