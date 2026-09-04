import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { SessionService } from './session.service';

/** Без имени календарь показывать нечего — отправляем представиться. */
export const signedInGuard: CanActivateFn = () =>
  inject(SessionService).isSignedIn() || inject(Router).createUrlTree(['/login']);
