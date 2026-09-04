import { ChangeDetectionStrategy, Component, effect, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AppHeader } from './layout/app-header/app-header';
import { ReminderToast } from './features/reminders/reminder-toast/reminder-toast';
import { ReminderService } from './features/reminders/reminder.service';
import { SessionService } from './core/session/session.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, AppHeader, ReminderToast],
  templateUrl: './app.html',
  styleUrl: './app.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {
  private readonly session = inject(SessionService);
  private readonly reminders = inject(ReminderService);

  constructor() {
    // Планировщик напоминаний работает всё время, пока человек представлен,
    // а не только пока открыт календарь.
    effect(() => {
      if (this.session.isSignedIn()) {
        this.reminders.start();
      } else {
        this.reminders.stop();
      }
    });
  }
}
