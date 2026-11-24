import { describe, expect, it } from 'vitest';
import type { PlannedWorkoutResponse, WorkoutResponse } from '../../api/training/types.gen';
import { moveRequest, toCalendarEvents } from './calendarEvents';

/**
 * Egy tervezett edzés a megadott állapottal.
 */
function plannedItem(id: string, status: PlannedWorkoutResponse['status']): PlannedWorkoutResponse {
  return { id, planId: 'p1', runnerId: 'r', date: '2026-11-25', type: 'tempo', plannedDistanceKm: 10, plannedDuration: null, targetRpe: 7, note: 'iramváltás', status };
}

const workout: WorkoutResponse = { id: 'w1', runnerId: 'r', date: '2026-11-24', type: 'easy', distanceKm: 8, duration: '00:45:00', paceSecondsPerKm: 338, rpe: 4, load: 180, createdAt: '', updatedAt: '' };

/** A naptár eseményei. */
describe('calendarEvents', () => {
  it('a tervezett és kihagyott edzés húzható, a teljesített tervezett nem jelenik meg, az elvégzett ✓ jellel', () => {
    const events = toCalendarEvents([plannedItem('a', 'planned'), plannedItem('b', 'missed'), plannedItem('c', 'completed')], [workout]);

    expect(events.map((event) => [event.id, event.title, event.editable])).toEqual([
      ['planned:a', 'Tempó · 10 km · RPE 7', true],
      ['planned:b', 'Kihagyott: Tempó · 10 km · RPE 7', true],
      ['workout:w1', '✓ Könnyű · 8 km', false],
    ]);
  });

  it('áthelyezéskor csak a dátum változik', () => {
    expect(moveRequest(plannedItem('a', 'missed'), '2026-11-27')).toEqual({
      date: '2026-11-27',
      type: 'tempo',
      plannedDistanceKm: 10,
      plannedDuration: null,
      targetRpe: 7,
      note: 'iramváltás',
    });
  });
});
