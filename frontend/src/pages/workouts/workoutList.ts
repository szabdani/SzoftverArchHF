import type { WorkoutResponse, WorkoutType } from '../../api/training/types.gen';

/** A lista rendezési szempontja. */
export type WorkoutSort = { field: 'date' | 'distanceKm' | 'load'; descending: boolean };

/**
 * Az edzések típus szerinti szűrése (null: mind) és rendezése; a kliensen történik, mert egy futónak
 * kevés edzése van egy időszakban. Azonos kulcsnál a dátum (legújabb elöl) dönt.
 */
export function filterAndSortWorkouts(workouts: WorkoutResponse[], type: WorkoutType | null, sort: WorkoutSort): WorkoutResponse[] {
  const direction = sort.descending ? -1 : 1;

  return workouts
    .filter((workout) => type === null || workout.type === type)
    .toSorted((a, b) => {
      const primary = sort.field === 'date' ? a.date.localeCompare(b.date) : a[sort.field] - b[sort.field];
      return primary !== 0 ? primary * direction : b.date.localeCompare(a.date);
    });
}
