import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { SessionService } from '../../core/session/session.service';

/**
 * Экран входа этапа 2: ни паролей, ни SSO — только имя. Одно и то же имя всегда
 * приводит в одно и то же рабочее пространство, поэтому продолжить тестирование
 * можно с любого браузера.
 */
@Component({
  selector: 'app-login-page',
  imports: [FormsModule],
  templateUrl: './login-page.html',
  styleUrl: './login-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginPage {
  private readonly session = inject(SessionService);
  private readonly router = inject(Router);

  protected readonly name = signal('');
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);

  protected submit(): void {
    const name = this.name().trim();
    if (name.length === 0 || this.busy()) {
      this.error.set(name.length === 0 ? 'Введите имя' : null);
      return;
    }

    this.busy.set(true);
    this.error.set(null);

    this.session.signIn(name).subscribe({
      next: () => this.router.navigate(['/calendar']),
      error: () => {
        this.busy.set(false);
        this.error.set('Календарь не отвечает. Проверьте, что бэкенд поднят.');
      },
    });
  }
}
