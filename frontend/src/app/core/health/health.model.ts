/** Ответ `GET /api/health` бэкенда. */
export interface Health {
  status: 'healthy' | 'degraded';
  database: 'up' | 'down';
  version: string;
  utcNow: string;
}
