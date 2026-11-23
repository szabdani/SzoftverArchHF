import type { EventInput } from '@fullcalendar/react';
import type { PlannedWorkoutRequest, PlannedWorkoutResponse, WorkoutResponse } from '../../api/training/types.gen';
import { formatKm, workoutTypeLabels } from '../../lib/format';

/** A naptár színei: elvégzett edzés, tervezett és kihagyott tervezett edzés. */
const colors = { done: '#0ca678', planned: '#228be6', missed: '#fa5252' };

/**
 * A naptár eseményei (UC07): az elvégzett edzések (✓ jellel) és a még nem teljesült tervezett edzések,
 * eltérő színnel. A teljesített tervezett edzés helyett az elvégzett edzés látszik, hogy ne legyen dupla.
 * Csak a tervezett edzések húzhatók (drag &amp; drop áthelyezés).
 */
export function toCalendarEvents(planned: PlannedWorkoutResponse[], workouts: WorkoutResponse[]): EventInput[] {
  const plannedEvents: EventInput[] = planned
    .filter((item) => item.status !== 'completed')
    .map((item) => ({
      id: `planned:${item.id}`,
      title: `${item.status === 'missed' ? 'Kihagyott: ' : ''}${workoutTypeLabels[item.type]} · ${plannedAmount(item)} · RPE ${item.targetRpe}`,
      start: item.date,
      allDay: true,
      editable: true,
      color: item.status === 'missed' ? colors.missed : colors.planned,
      extendedProps: { planned: item },
    }));

  const workoutEvents: EventInput[] = workouts.map((workout) => ({
    id: `workout:${workout.id}`,
    title: `✓ ${workoutTypeLabels[workout.type]} · ${formatKm(workout.distanceKm)}`,
    start: workout.date,
    allDay: true,
    editable: false,
    color: colors.done,
  }));

  return [...plannedEvents, ...workoutEvents];
}

/**
 * A tervezett mennyiség röviden: táv és/vagy időtartam.
 */
export function plannedAmount(item: Pick<PlannedWorkoutResponse, 'plannedDistanceKm' | 'plannedDuration'>): string {
  return [item.plannedDistanceKm ? formatKm(item.plannedDistanceKm) : null, item.plannedDuration].filter(Boolean).join(' / ');
}

/**
 * Áthelyezéskor (drag &amp; drop) küldendő kérés: minden marad, csak a dátum változik.
 */
export function moveRequest(item: PlannedWorkoutResponse, date: string): PlannedWorkoutRequest {
  return {
    date,
    type: item.type,
    plannedDistanceKm: item.plannedDistanceKm ?? null,
    plannedDuration: item.plannedDuration ?? null,
    targetRpe: item.targetRpe,
    note: item.note ?? null,
  };
}
