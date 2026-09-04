import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Health } from './health.model';

/**
 * Единственный на этапе 1 вызов к бэкенду. Существует затем, чтобы приёмка
 * «фронт достучался до API» проверялась глазами, а не курлом.
 */
@Injectable({ providedIn: 'root' })
export class HealthService {
  private readonly http = inject(HttpClient);

  get(): Observable<Health> {
    return this.http.get<Health>('/api/health');
  }
}
