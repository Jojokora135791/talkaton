import { Routes } from '@angular/router';
import { signedInGuard } from './core/session/session.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/login/login-page').then((m) => m.LoginPage),
  },
  {
    path: 'calendar',
    canActivate: [signedInGuard],
    loadComponent: () => import('./features/calendar/calendar-page').then((m) => m.CalendarPage),
  },
  { path: '', pathMatch: 'full', redirectTo: 'calendar' },
  { path: '**', redirectTo: 'calendar' },
];
