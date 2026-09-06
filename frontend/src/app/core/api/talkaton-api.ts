import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, of } from 'rxjs';
import {
  Artifact,
  Calendar,
  CreateEventRequest,
  EditScope,
  EventDetails,
  Health,
  Occurrence,
  ParticipantList,
  ParticipantStatus,
  Room,
  RoomAvailability,
  UpdateEventRequest,
  User,
  UserAvailability,
} from './models';

/**
 * Единственное место, которое знает URL-ы бэкенда. Компоненты ходят только сюда,
 * поэтому на этапе 6 переезд на API Толка — это правка одного файла.
 */
@Injectable({ providedIn: 'root' })
export class TalkatonApi {
  private readonly http = inject(HttpClient);

  health(): Observable<Health> {
    return this.http.get<Health>('/api/health');
  }

  signIn(name: string): Observable<User> {
    return this.http.post<User>('/api/session', {
      name,
      timeZoneId: Intl.DateTimeFormat().resolvedOptions().timeZone,
      // Смещение на восток от UTC: у JS знак обратный, поэтому минус.
      utcOffsetMinutes: -new Date().getTimezoneOffset(),
    });
  }

  currentUser(): Observable<User> {
    return this.http.get<User>('/api/session');
  }

  calendars(): Observable<Calendar[]> {
    return this.http.get<Calendar[]>('/api/calendars');
  }

  updateCalendar(id: string, patch: Partial<Pick<Calendar, 'name' | 'color' | 'isVisible'>>): Observable<Calendar> {
    return this.http.patch<Calendar>(`/api/calendars/${id}`, patch);
  }

  events(fromUtc: string, toUtc: string, calendarIds?: string[]): Observable<Occurrence[]> {
    let params = new HttpParams().set('from', fromUtc).set('to', toUtc);
    if (calendarIds?.length) {
      params = params.set('calendarIds', calendarIds.join(','));
    }

    return this.http.get<Occurrence[]>('/api/events', { params });
  }

  eventDetails(eventId: string, occurrenceStartUtc?: string): Observable<EventDetails> {
    const params = occurrenceStartUtc
      ? new HttpParams().set('occurrenceStart', occurrenceStartUtc)
      : undefined;

    return this.http.get<EventDetails>(`/api/events/${eventId}`, { params });
  }

  eventArtifacts(eventId: string): Observable<Artifact[]> {
    return this.http.get<Artifact[]>(`/api/events/${eventId}/artifacts`);
  }

  createEvent(request: CreateEventRequest): Observable<EventDetails> {
    return this.http.post<EventDetails>('/api/events', request);
  }

  updateEvent(
    eventId: string,
    request: UpdateEventRequest,
    scope: EditScope = 'series',
    occurrenceStartUtc?: string,
  ): Observable<EventDetails> {
    return this.http.patch<EventDetails>(
      `/api/events/${eventId}`,
      request,
      { params: this.scopeParams(scope, occurrenceStartUtc) },
    );
  }

  deleteEvent(eventId: string, scope: EditScope = 'series', occurrenceStartUtc?: string): Observable<void> {
    return this.http.delete<void>(`/api/events/${eventId}`, {
      params: this.scopeParams(scope, occurrenceStartUtc),
    });
  }

  rsvp(eventId: string, status: ParticipantStatus): Observable<EventDetails> {
    return this.http.post<EventDetails>(`/api/events/${eventId}/rsvp`, { status });
  }

  /** Грид занятости (Этап 7.1): busy-интервалы каждого из `userIds` за период. */
  availability(userIds: string[], fromUtc: string, toUtc: string): Observable<UserAvailability[]> {
    if (userIds.length === 0) {
      return of([]);
    }

    const params = new HttpParams().set('userIds', userIds.join(',')).set('from', fromUtc).set('to', toUtc);
    return this.http.get<UserAvailability[]>('/api/availability', { params });
  }

  /** Список переговорок (Этап 7.2) для выбора при создании встречи. */
  rooms(): Observable<Room[]> {
    return this.http.get<Room[]>('/api/rooms');
  }

  /** Грид занятости переговорок за период — тот же принцип, что и `availability`. */
  roomAvailability(roomIds: string[], fromUtc: string, toUtc: string): Observable<RoomAvailability[]> {
    if (roomIds.length === 0) {
      return of([]);
    }

    const params = new HttpParams().set('roomIds', roomIds.join(',')).set('from', fromUtc).set('to', toUtc);
    return this.http.get<RoomAvailability[]>('/api/rooms/availability', { params });
  }

  users(query?: string): Observable<User[]> {
    const params = query ? new HttpParams().set('query', query) : undefined;
    return this.http.get<User[]>('/api/users', { params });
  }

  participantLists(): Observable<ParticipantList[]> {
    return this.http.get<ParticipantList[]>('/api/participant-lists');
  }

  createParticipantList(name: string, color: string, memberIds: string[]): Observable<ParticipantList> {
    return this.http.post<ParticipantList>('/api/participant-lists', { name, color, memberIds });
  }

  deleteParticipantList(id: string): Observable<void> {
    return this.http.delete<void>(`/api/participant-lists/${id}`);
  }

  private scopeParams(scope: EditScope, occurrenceStartUtc?: string): HttpParams {
    let params = new HttpParams().set('scope', scope);
    if (occurrenceStartUtc) {
      params = params.set('occurrenceStart', occurrenceStartUtc);
    }

    return params;
  }
}
