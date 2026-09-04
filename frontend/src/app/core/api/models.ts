/** Контракты `/api`. Один в один с DTO бэкенда — руками ничего не домысливаем. */

export interface User {
  id: string;
  displayName: string;
  timeZoneId: string;
  avatarColorIndex: number;
}

export interface Calendar {
  id: string;
  name: string;
  color: string;
  isVisible: boolean;
  sortOrder: number;
}

export type ParticipantStatus = 'accepted' | 'declined' | 'tentative';

export type ArtifactKind = 'recording' | 'protocol' | 'board' | 'tasks';

/** Одно вхождение встречи в сетке. У разовой встречи оно единственное. */
export interface Occurrence {
  eventId: string;
  /** Ключ вхождения внутри серии — с ним возвращаемся в PATCH и DELETE. */
  occurrenceStartUtc: string;
  startUtc: string;
  endUtc: string;
  title: string;
  isAllDay: boolean;
  calendarId: string;
  calendarName: string;
  calendarColor: string;
  recurrenceRule: string | null;
  isMoved: boolean;
  talkRoomSlug: string | null;
  organizerId: string;
  organizerName: string;
  isOrganizer: boolean;
  myStatus: ParticipantStatus | null;
  participantCount: number;
  artifactCount: number;
  reminderMinutesBefore: number | null;
}

export interface Participant {
  userId: string;
  displayName: string;
  status: ParticipantStatus;
  isOrganizer: boolean;
  avatarColorIndex: number;
}

export interface Artifact {
  id: string;
  kind: ArtifactKind;
  title: string;
  subtitle: string | null;
  url: string | null;
}

export interface EventDetails {
  occurrence: Occurrence;
  description: string | null;
  participants: Participant[];
  artifacts: Artifact[];
}

export interface ParticipantList {
  id: string;
  name: string;
  sortOrder: number;
  members: User[];
}

export interface CreateEventRequest {
  calendarId: string;
  title: string;
  description?: string | null;
  startUtc: string;
  endUtc: string;
  isAllDay: boolean;
  recurrenceRule?: string | null;
  talkRoomSlug?: string | null;
  participantIds?: string[];
  reminderMinutesBefore?: number | null;
}

export interface UpdateEventRequest {
  calendarId?: string;
  title?: string;
  description?: string | null;
  startUtc?: string;
  endUtc?: string;
  isAllDay?: boolean;
  recurrenceRule?: string | null;
  clearRecurrence?: boolean;
  talkRoomSlug?: string | null;
  participantIds?: string[];
  reminderMinutesBefore?: number;
}

/** К чему относится правка: ко всей серии или к одному вхождению. */
export type EditScope = 'series' | 'occurrence';

export interface Health {
  status: 'healthy' | 'degraded';
  database: 'up' | 'down';
  version: string;
  utcNow: string;
}
