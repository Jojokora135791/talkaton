import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  afterNextRender,
  computed,
  inject,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { Occurrence } from '../../../core/api/models';
import { OccurrenceKey, isSameOccurrence } from '../calendar-store';
import {
  MINUTES_IN_DAY,
  addDays,
  addMinutes,
  clamp,
  dayKey,
  formatTime,
  formatWeekdayShort,
  isToday,
  minutesFromMidnight,
  startOfDay,
} from '../../../core/time/date-utils';

/** Шаг сетки при перетаскивании: пятиминутки достаточно, а попадать по ней легко. */
const SNAP_MINUTES = 5;

/** Меньше этого встреча превращается в полоску без текста. */
const MIN_DURATION_MINUTES = 15;

/** Сколько пикселей нужно проехать, чтобы это считалось перетаскиванием, а не щелчком. */
const DRAG_THRESHOLD_PX = 3;

/** Как часто двигается линия текущего времени. */
const NOW_TICK_MS = 30_000;

/** Если сегодняшнего дня в периоде нет, открываемся на рабочем утре, а не на полуночи. */
const INITIAL_SCROLL_HOUR = 7;

/** Сколько места оставить над точкой прокрутки, чтобы она не липла к верхней границе. */
const SCROLL_HEADROOM_PX = 90;

export interface OccurrenceMove {
  occurrence: Occurrence;
  start: Date;
  end: Date;
}

interface PositionedEvent {
  occurrence: Occurrence;
  key: string;
  /** Проценты от высоты суток — так сетка не зависит от пиксельной высоты часа. */
  top: number;
  height: number;
  left: number;
  width: number;
  timeLabel: string;
  selected: boolean;
  dragging: boolean;
}

interface DayColumn {
  date: Date;
  key: string;
  weekday: string;
  dayNumber: number;
  today: boolean;
  events: PositionedEvent[];
  allDay: Occurrence[];
}

interface DragState {
  occurrence: Occurrence;
  mode: 'move' | 'resize';
  originX: number;
  originY: number;
  start: Date;
  end: Date;
  moved: boolean;
}

/**
 * Сетка дня и недели: 24 часа, полоса «весь день», линия текущего времени,
 * перетаскивание и растягивание встреч.
 *
 * Вид «День» — та же сетка с одной колонкой, отдельного компонента не заводим.
 */
@Component({
  selector: 'app-week-grid',
  templateUrl: './week-grid.html',
  styleUrl: './week-grid.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeekGrid {
  private readonly destroyRef = inject(DestroyRef);

  readonly days = input.required<readonly Date[]>();
  readonly occurrences = input.required<readonly Occurrence[]>();
  readonly selected = input<OccurrenceKey | null>(null);

  readonly occurrenceSelected = output<Occurrence>();
  readonly occurrenceMoved = output<OccurrenceMove>();
  readonly slotPicked = output<Date>();

  private readonly columnsRef = viewChild.required<ElementRef<HTMLElement>>('columnsHost');
  private readonly scrollRef = viewChild.required<ElementRef<HTMLElement>>('scroller');

  private readonly drag = signal<DragState | null>(null);
  protected readonly preview = signal<{ start: Date; end: Date } | null>(null);
  private readonly now = signal(new Date());

  protected readonly hours = Array.from({ length: 24 }, (_, hour) => `${hour}`.padStart(2, '0') + ':00');

  protected readonly columns = computed<DayColumn[]>(() => {
    const selected = this.selected();
    const dragging = this.drag()?.occurrence ?? null;

    return this.days().map((date) => {
      const dayStart = startOfDay(date);
      const dayEnd = addDays(dayStart, 1);
      const inDay: PositionedEvent[] = [];
      const allDay: Occurrence[] = [];

      for (const occurrence of this.occurrences()) {
        const { start, end } = this.timesOf(occurrence);

        if (end <= dayStart || start >= dayEnd) {
          continue;
        }

        if (occurrence.isAllDay) {
          allDay.push(occurrence);
          continue;
        }

        // Встречу через полночь режем по суткам: в каждой колонке своя часть.
        const from = start < dayStart ? dayStart : start;
        const to = end > dayEnd ? dayEnd : end;

        inDay.push({
          occurrence,
          key: `${occurrence.eventId}@${occurrence.occurrenceStartUtc}@${dayKey(date)}`,
          top: (minutesFromMidnight(from) / MINUTES_IN_DAY) * 100,
          height: Math.max(
            ((to.getTime() - from.getTime()) / 60_000 / MINUTES_IN_DAY) * 100,
            (MIN_DURATION_MINUTES / MINUTES_IN_DAY) * 100,
          ),
          left: 0,
          width: 100,
          timeLabel: `${formatTime(start)} – ${formatTime(end)}`,
          selected: isSameOccurrence(occurrence, selected),
          dragging: dragging !== null && isSameOccurrence(occurrence, dragging),
        });
      }

      spreadOverlapping(inDay);

      return {
        date,
        key: dayKey(date),
        weekday: formatWeekdayShort(date),
        dayNumber: date.getDate(),
        today: isToday(date),
        events: inDay,
        allDay,
      };
    });
  });

  /** Полосу «весь день» показываем, только если в ней что-то есть. */
  protected readonly hasAllDay = computed(() => this.columns().some((column) => column.allDay.length > 0));

  protected readonly nowLineTop = computed(() => (minutesFromMidnight(this.now()) / MINUTES_IN_DAY) * 100);

  protected readonly nowLabel = computed(() => formatTime(this.now()));

  protected readonly showNowLine = computed(() => this.days().some((date) => isToday(date)));

  constructor() {
    const timer = setInterval(() => this.now.set(new Date()), NOW_TICK_MS);
    this.destroyRef.onDestroy(() => clearInterval(timer));

    // Полночь вверху сетки никому не нужна: открываемся на текущем времени,
    // а если сегодня не в периоде — на рабочем утре. Кадр ожидания нужен, чтобы
    // высота колонок была уже посчитана.
    afterNextRender(() => {
      requestAnimationFrame(() => {
        const scroller = this.scrollRef().nativeElement;
        const minutes = this.showNowLine() ? minutesFromMidnight(new Date()) : INITIAL_SCROLL_HOUR * 60;
        const target = (this.columnsRef().nativeElement.clientHeight * minutes) / MINUTES_IN_DAY;
        scroller.scrollTop = Math.max(0, target - SCROLL_HEADROOM_PX);
      });
    });
  }

  protected onPointerDown(event: PointerEvent, item: PositionedEvent): void {
    if (event.button !== 0) {
      return;
    }

    // Захват ставим на саму карточку, а не на ручку: тогда move и up приходят в один обработчик.
    const mode: 'move' | 'resize' =
      (event.target as HTMLElement).classList.contains('event__resize') ? 'resize' : 'move';

    const { start, end } = this.timesOf(item.occurrence);
    this.drag.set({
      occurrence: item.occurrence,
      mode,
      originX: event.clientX,
      originY: event.clientY,
      start,
      end,
      moved: false,
    });
    this.preview.set({ start, end });

    (event.currentTarget as HTMLElement).setPointerCapture(event.pointerId);
    event.stopPropagation();
    event.preventDefault();
  }

  protected onPointerMove(event: PointerEvent): void {
    const drag = this.drag();
    if (!drag) {
      return;
    }

    const rect = this.columnsRef().nativeElement.getBoundingClientRect();
    const dx = event.clientX - drag.originX;
    const dy = event.clientY - drag.originY;

    if (!drag.moved && Math.abs(dx) < DRAG_THRESHOLD_PX && Math.abs(dy) < DRAG_THRESHOLD_PX) {
      return;
    }

    const minutesPerPixel = MINUTES_IN_DAY / rect.height;
    const deltaMinutes = Math.round((dy * minutesPerPixel) / SNAP_MINUTES) * SNAP_MINUTES;

    if (drag.mode === 'resize') {
      const end = addMinutes(drag.end, deltaMinutes);
      const earliest = addMinutes(drag.start, MIN_DURATION_MINUTES);
      this.preview.set({ start: drag.start, end: end < earliest ? earliest : end });
    } else {
      const dayWidth = rect.width / this.days().length;
      const deltaDays = dayWidth > 0 ? Math.round(dx / dayWidth) : 0;
      const shift = deltaMinutes + deltaDays * MINUTES_IN_DAY;
      this.preview.set({ start: addMinutes(drag.start, shift), end: addMinutes(drag.end, shift) });
    }

    this.drag.set({ ...drag, moved: true });
  }

  protected onPointerUp(event: PointerEvent): void {
    const drag = this.drag();
    const preview = this.preview();
    this.drag.set(null);
    this.preview.set(null);

    if (!drag) {
      return;
    }

    const card = event.currentTarget as HTMLElement;
    if (card.hasPointerCapture(event.pointerId)) {
      card.releasePointerCapture(event.pointerId);
    }

    if (!drag.moved || !preview) {
      this.occurrenceSelected.emit(drag.occurrence);
      return;
    }

    this.occurrenceMoved.emit({ occurrence: drag.occurrence, start: preview.start, end: preview.end });
  }

  protected readonly formatTime = formatTime;

  protected onAllDaySelected(occurrence: Occurrence): void {
    this.occurrenceSelected.emit(occurrence);
  }

  protected isBirthday(item: Occurrence): boolean {
    return (
      item.calendarName.toLowerCase().includes('рождения') ||
      item.title.toLowerCase().includes('день рождения') ||
      (item.talkRoomSlug?.startsWith('bday:') ?? false)
    );
  }

  protected birthdayName(title: string): string {
    return title.replace(/^День рождения\s*[—–-]?\s*/i, '').trim() || title;
  }

  protected isVacation(item: Occurrence): boolean {
    return item.talkRoomSlug === 'vacation' || item.title.toLowerCase().startsWith('отпуск');
  }

  /** Двойной щелчок по пустому месту — создать встречу на этот час. */
  protected onColumnDoubleClick(event: MouseEvent, column: DayColumn): void {
    const target = event.currentTarget as HTMLElement;
    const offsetY = event.clientY - target.getBoundingClientRect().top;
    const minutes = clamp(
      Math.round(((offsetY / target.clientHeight) * MINUTES_IN_DAY) / 30) * 30,
      0,
      MINUTES_IN_DAY - 60,
    );

    this.slotPicked.emit(addMinutes(startOfDay(column.date), minutes));
  }

  /** Времена вхождения с учётом перетаскивания: карточка должна ехать за курсором. */
  private timesOf(occurrence: Occurrence): { start: Date; end: Date } {
    const drag = this.drag();
    const preview = this.preview();

    if (drag && preview && isSameOccurrence(occurrence, drag.occurrence)) {
      return preview;
    }

    return { start: new Date(occurrence.startUtc), end: new Date(occurrence.endUtc) };
  }
}

/**
 * Раскладывает пересекающиеся встречи по дорожкам. Сначала режем день на кластеры
 * связанных пересечений, потом внутри кластера каждой встрече ищем первую свободную дорожку —
 * так две параллельные встречи делят ширину пополам, а не наезжают друг на друга.
 */
function spreadOverlapping(events: PositionedEvent[]): void {
  events.sort((left, right) => left.top - right.top || right.height - left.height);

  let cluster: PositionedEvent[] = [];
  let clusterEnd = -1;

  const flush = (): void => {
    if (cluster.length === 0) {
      return;
    }

    const laneEnds: number[] = [];
    const lanes = new Map<PositionedEvent, number>();

    for (const item of cluster) {
      let lane = laneEnds.findIndex((end) => end <= item.top + 0.0001);
      if (lane < 0) {
        lane = laneEnds.length;
      }

      laneEnds[lane] = item.top + item.height;
      lanes.set(item, lane);
    }

    const width = 100 / laneEnds.length;
    for (const item of cluster) {
      const lane = lanes.get(item) ?? 0;
      item.left = lane * width;
      item.width = width;
    }

    cluster = [];
    clusterEnd = -1;
  };

  for (const item of events) {
    if (cluster.length > 0 && item.top >= clusterEnd) {
      flush();
    }

    cluster.push(item);
    clusterEnd = Math.max(clusterEnd, item.top + item.height);
  }

  flush();
}
