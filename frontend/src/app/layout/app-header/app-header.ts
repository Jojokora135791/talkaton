import { ChangeDetectionStrategy, Component } from '@angular/core';

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
  protected readonly tabs: readonly NavTab[] = [
    { label: 'Календарь', icon: '🗓', active: true },
    { label: 'Чаты', icon: '💬', active: false, badge: 1 },
    { label: 'Артефакты', icon: '📁', active: false },
    { label: 'Доски', icon: '🗂', active: false },
    { label: 'Контакты', icon: '👥', active: false },
  ];
}
