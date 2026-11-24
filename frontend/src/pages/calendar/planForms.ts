import dayjs from 'dayjs';
import type { PlannedWorkoutRequest, PlannedWorkoutResponse, PlanRequest, PlanResponse, WorkoutType } from '../../api/training/types.gen';
import { zPlannedWorkoutRequest, zPlanRequest } from '../../api/training/zod.gen';
import { formatSeconds, parseDuration } from '../../lib/format';
import { schemaErrors } from '../../lib/schemaErrors';

/** A terv-űrlap értékei. */
export type PlanFormValues = { name: string; startDate: string | null; endDate: string | null; linkActiveGoal: boolean };

/** A tervezett edzés űrlapjának értékei; az üres szám mező ''. */
export type PlannedWorkoutFormValues = {
  planId: string | null;
  date: string | null;
  type: WorkoutType | null;
  plannedDistanceKm: number | string;
  plannedDuration: string;
  targetRpe: number | string;
  note: string;
};

const planMessages: Record<string, string> = {
  name: 'A terv neve kötelező (legfeljebb 100 karakter).',
  startDate: 'A kezdődátum megadása kötelező.',
  endDate: 'A záródátum megadása kötelező.',
};

const plannedMessages: Record<string, string> = {
  date: 'A dátum megadása kötelező.',
  type: 'Válassz edzéstípust.',
  plannedDistanceKm: 'A táv 0,1 és 100 km között lehet.',
  plannedDuration: 'Az időtartam 1 perc és 10 óra között lehet, óó:pp:mm formátumban.',
  targetRpe: 'A cél RPE 1 és 10 közötti egész szám lehet.',
  note: 'A megjegyzés legfeljebb 500 karakter lehet.',
};

/**
 * Egy terv űrlap-értékei (új tervnél üres, az aktív célhoz kötve).
 */
export function planToFormValues(plan?: PlanResponse): PlanFormValues {
  return plan
    ? { name: plan.name, startDate: plan.startDate, endDate: plan.endDate, linkActiveGoal: plan.goalId != null }
    : { name: '', startDate: null, endDate: null, linkActiveGoal: true };
}

/**
 * A terv ellenőrzése a közös sémával, plusz: a záródátum nem korábbi a kezdőnél, és legfeljebb egy év.
 */
export function validatePlan(values: PlanFormValues): Record<string, string> {
  const errors = schemaErrors(zPlanRequest, toPlanCandidate(values, undefined), planMessages);

  if (values.startDate && values.endDate) {
    if (values.endDate < values.startDate) {
      errors.endDate ??= 'A záródátum nem lehet korábbi a kezdődátumnál.';
    } else if (dayjs(values.endDate).diff(values.startDate, 'day') > 366) {
      errors.endDate ??= 'Egy terv legfeljebb egy évre szólhat.';
    }
  }

  return errors;
}

/**
 * A szervernek küldendő terv; az edző a futó azonosítójával készíti a futónak.
 */
export function toPlanRequest(values: PlanFormValues, runnerId: string | undefined): PlanRequest {
  return zPlanRequest.parse(toPlanCandidate(values, runnerId));
}

/**
 * Egy tervezett edzés űrlap-értékei: meglévőnél az adatai, újnál a megadott terv és nap.
 */
export function plannedToFormValues(item: PlannedWorkoutResponse | undefined, planId: string | null, date: string | null): PlannedWorkoutFormValues {
  return item
    ? {
        planId: item.planId,
        date: item.date,
        type: item.type,
        plannedDistanceKm: item.plannedDistanceKm ?? '',
        plannedDuration: item.plannedDuration ?? '',
        targetRpe: item.targetRpe,
        note: item.note ?? '',
      }
    : { planId, date, type: null, plannedDistanceKm: '', plannedDuration: '', targetRpe: '', note: '' };
}

/**
 * A tervezett edzés ellenőrzése a közös sémával, plusz: kell terv, a dátum a terv időszakába esik, és a
 * táv vagy az időtartam közül legalább az egyik meg van adva.
 */
export function validatePlannedWorkout(values: PlannedWorkoutFormValues, plan: PlanResponse | undefined): Record<string, string> {
  const errors = schemaErrors(zPlannedWorkoutRequest, toPlannedCandidate(values), plannedMessages);

  if (!values.planId) {
    errors.planId = 'Válassz tervet.';
  }

  if (plan && values.date && (values.date < plan.startDate || values.date > plan.endDate)) {
    errors.date ??= 'A dátumnak a terv időszakába kell esnie.';
  }

  if (values.plannedDistanceKm === '' && values.plannedDuration.trim() === '') {
    errors.plannedDistanceKm ??= 'A tervezett táv vagy időtartam közül legalább az egyiket meg kell adni.';
  }

  return errors;
}

/**
 * A szervernek küldendő tervezett edzés.
 */
export function toPlannedWorkoutRequest(values: PlannedWorkoutFormValues): PlannedWorkoutRequest {
  return zPlannedWorkoutRequest.parse(toPlannedCandidate(values));
}

/**
 * A terv kérése validálás előtt.
 */
function toPlanCandidate(values: PlanFormValues, runnerId: string | undefined): Record<string, unknown> {
  return {
    runnerId: runnerId ?? null,
    name: values.name.trim(),
    startDate: values.startDate ?? undefined,
    endDate: values.endDate ?? undefined,
    linkActiveGoal: values.linkActiveGoal,
  };
}

/**
 * A tervezett edzés kérése validálás előtt; az időtartam óó:pp:mm alakra igazodik.
 */
function toPlannedCandidate(values: PlannedWorkoutFormValues): Record<string, unknown> {
  const duration = values.plannedDuration.trim();
  const seconds = duration === '' ? null : parseDuration(duration);
  return {
    date: values.date ?? undefined,
    type: values.type ?? undefined,
    plannedDistanceKm: values.plannedDistanceKm === '' ? null : Number(values.plannedDistanceKm),
    plannedDuration: duration === '' ? null : seconds === null ? duration : formatSeconds(seconds),
    targetRpe: values.targetRpe === '' ? undefined : Number(values.targetRpe),
    note: values.note.trim() === '' ? null : values.note.trim(),
  };
}
