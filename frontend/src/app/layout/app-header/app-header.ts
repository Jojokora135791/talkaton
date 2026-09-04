import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import { SessionService } from '../../core/session/session.service';
import { ReminderService } from '../../features/reminders/reminder.service';

interface NavTab {
  readonly label: string;
  readonly icon: string;
  /** Пока активен только «Календарь» — остальные вкладки живут в самом Толке. */
  readonly active: boolean;
  readonly badge?: number;
}

@Component({
  selector: 'app-header',
  templateUrl: './app-header.html',
  styleUrl: './app-header.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppHeader {
  private readonly session = inject(SessionService);
  private readonly reminders = inject(ReminderService);
  private readonly router = inject(Router);

  protected readonly tabs: readonly NavTab[] = [
    { label: 'Календарь', icon: '🗓', active: true },
    { label: 'Чаты', icon: '💬', active: false, badge: 1 },
    { label: 'Артефакты', icon: '📁', active: false },
    { label: 'Доски', icon: '🗂', active: false },
    { label: 'Контакты', icon: '👥', active: false },
  ];

  protected readonly user = this.session.user;

  /** Кнопку показываем, только пока разрешение ещё не спрошено: жест человека обязателен. */
  protected readonly canAskForNotifications = computed(
    () => this.session.isSignedIn() && this.reminders.permission() === 'default',
  );

  protected readonly initials = computed(() => {
    const name = this.user()?.displayName ?? '';
    return name
      .split(' ')
      .filter((part) => part.length > 0)
      .slice(0, 2)
      .map((part) => part[0].toUpperCase())
      .join('');
  });

  protected enableNotifications(): void {
    this.reminders.requestPermission();
  }

  protected signOut(): void {
    this.session.signOut();
    void this.router.navigate(['/login']);
  }
}
