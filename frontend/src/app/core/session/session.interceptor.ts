import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { SessionService } from './session.service';

/** Заголовок, по которому бэкенд узнаёт вошедшего. На этапе 6 его место займёт токен SSO. */
export const USER_HEADER = 'X-Talkaton-User';

export const sessionInterceptor: HttpInterceptorFn = (request, next) => {
  const user = inject(SessionService).user();
  if (!user || !request.url.startsWith('/api')) {
    return next(request);
  }

  return next(request.clone({ setHeaders: { [USER_HEADER]: user.id } }));
};
