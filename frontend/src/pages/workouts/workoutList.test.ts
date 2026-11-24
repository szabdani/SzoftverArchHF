import { describe, expect, it } from 'vitest';
import type { WorkoutResponse } from '../../api/training/types.gen';
import { filterAndSortWorkouts } from './workoutList';

/**
 * Egy tesztedzés a megadott dátummal, típussal, távval és terheléssel.
 */
function workout(id: string, date: string, type: WorkoutResponse['type'], distanceKm: number, load: number): WorkoutResponse {
  return { id, runnerId: 'r', date, type, distanceKm, duration: '00:40:00', paceSecondsPerKm: 300, rpe: 4, load, createdAt: '', updatedAt: '' };
}

const workouts = [
  workout('a', '2026-11-20', 'easy', 8, 200),
  workout('b', '2026-11-22', 'long', 20, 600),
  workout('c', '2026-11-21', 'easy', 12, 250),
];

/** Az edzésnapló szűrése és rendezése. */
describe('filterAndSortWorkouts', () => {
  it('alapból dátum szerint csökkenően', () => {
    expect(filterAndSortWorkouts(workouts, null, { field: 'date', descending: true }).map((w) => w.id)).toEqual(['b', 'c', 'a']);
  });

  it('táv és terhelés szerint mindkét irányba', () => {
    expect(filterAndSortWorkouts(workouts, null, { field: 'distanceKm', descending: false }).map((w) => w.id)).toEqual(['a', 'c', 'b']);
    expect(filterAndSortWorkouts(workouts, null, { field: 'load', descending: true }).map((w) => w.id)).toEqual(['b', 'c', 'a']);
  });

  it('típus szerint szűr', () => {
    expect(filterAndSortWorkouts(workouts, 'easy', { field: 'date', descending: true }).map((w) => w.id)).toEqual(['c', 'a']);
  });
});
