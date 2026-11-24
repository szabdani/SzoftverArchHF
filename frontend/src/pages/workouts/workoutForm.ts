import type { WorkoutRequest, WorkoutResponse, WorkoutType } from '../../api/training/types.gen';
import { zWorkoutRequest } from '../../api/training/zod.gen';
import { formatSeconds, parseDuration } from '../../lib/format';

/** Az edzés-űrlap értékei; az üres szám mező '' (a Mantine NumberInput így jelzi). */
export type WorkoutFormValues = {
  date: string | null;
  type: WorkoutType | null;
  distanceKm: number | string;
  duration: string;
  avgHeartRate: number | string;
  elevationGainM: number | string;
  rpe: number | string;
  note: string;
  plannedWorkoutId: string | null;
};

/**
 * A mezőnkénti magyar hibaüzenetek. Maguk a szabályok a szerverrel közös (generált) zod sémából jönnek,
 * itt csak a szövegük van.
 */
const fieldMessages: Record<string, string> = {
  date: 'A dátum megadása kötelező.',
  type: 'Válassz edzéstípust.',
  distanceKm: 'A táv 0,1 és 100 km között lehet.',
  duration: 'Az időtartam 1 perc és 10 óra között lehet, óó:pp:mm formátumban.',
  avgHeartRate: 'Az átlagpulzus 40 és 220 ütés/perc között lehet.',
  elevationGainM: 'Az emelkedés 0 és 5000 méter között lehet.',
  rpe: 'Az RPE 1 és 10 közötti egész szám lehet.',
  note: 'A megjegyzés legfeljebb 500 karakter lehet.',
};

/**
 * Üres űrlap új edzéshez, a mai dátummal.
 */
export function emptyWorkoutForm(today: string): WorkoutFormValues {
  return { date: today, type: null, distanceKm: '', duration: '', avgHeartRate: '', elevationGainM: '', rpe: '', note: '', plannedWorkoutId: null };
}

/**
 * Egy meglévő edzés űrlap-értékei (szerkesztéshez).
 */
export function workoutToFormValues(workout: WorkoutResponse): WorkoutFormValues {
  return {
    date: workout.date,
    type: workout.type,
    distanceKm: workout.distanceKm,
    duration: workout.duration,
    avgHeartRate: workout.avgHeartRate ?? '',
    elevationGainM: workout.elevationGainM ?? '',
    rpe: workout.rpe,
    note: workout.note ?? '',
    plannedWorkoutId: workout.plannedWorkoutId ?? null,
  };
}

/**
 * Az űrlap értékeiből összeállított kérés, még validálás előtt: az üres mezőkből null/hiányzó érték lesz,
 * az időtartam óó:pp:mm alakra igazodik (pl. 1:05:30 → 01:05:30).
 */
export function toRequestCandidate(values: WorkoutFormValues): Record<string, unknown> {
  const seconds = parseDuration(values.duration);
  return {
    date: values.date ?? undefined,
    type: values.type ?? undefined,
    distanceKm: values.distanceKm === '' ? undefined : Number(values.distanceKm),
    duration: seconds === null ? values.duration.trim() : formatSeconds(seconds),
    avgHeartRate: values.avgHeartRate === '' ? null : Number(values.avgHeartRate),
    elevationGainM: values.elevationGainM === '' ? null : Number(values.elevationGainM),
    rpe: values.rpe === '' ? undefined : Number(values.rpe),
    note: values.note.trim() === '' ? null : values.note.trim(),
    plannedWorkoutId: values.plannedWorkoutId,
  };
}

/**
 * Az űrlap ellenőrzése a szerverrel közös zod sémával, plusz a mai naptól függő szabállyal
 * (a dátum nem lehet jövőbeli; ezt a szerver is ellenőrzi).
 * @returns mezőnként a magyar hibaüzenet (üres, ha minden rendben).
 */
export function validateWorkout(values: WorkoutFormValues, today: string): Record<string, string> {
  const errors: Record<string, string> = {};
  const result = zWorkoutRequest.safeParse(toRequestCandidate(values));

  if (!result.success) {
    // Mezőnként az első hiba számít; a sémán kívüli mezőnél a zod saját üzenete marad.
    for (const issue of result.error.issues) {
      const field = String(issue.path[0]);
      errors[field] ??= fieldMessages[field] ?? issue.message;
    }
  }

  if (values.date && values.date > today) {
    errors.date ??= 'A dátum nem lehet jövőbeli.';
  }

  return errors;
}

/**
 * Az ellenőrzött űrlapból a szervernek küldendő kérés.
 */
export function toWorkoutRequest(values: WorkoutFormValues): WorkoutRequest {
  return zWorkoutRequest.parse(toRequestCandidate(values));
}
