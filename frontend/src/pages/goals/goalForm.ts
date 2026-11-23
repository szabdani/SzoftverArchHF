import type { GoalRequest, GoalResponse, GoalType } from '../../api/training/types.gen';
import { zGoalRequest } from '../../api/training/zod.gen';
import { formatSeconds, parseDuration } from '../../lib/format';
import { schemaErrors } from '../../lib/schemaErrors';

/** A cél-űrlap értékei; az üres szám mező ''. */
export type GoalFormValues = {
  type: GoalType | null;
  distanceKm: number | string;
  targetDate: string | null;
  targetTime: string;
};

/** Mezőnkénti magyar hibaüzenetek (a szabályok a közös sémából jönnek). */
const fieldMessages: Record<string, string> = {
  type: 'Válassz céltípust.',
  distanceKm: 'A táv 0,1 és 500 km között lehet.',
  targetDate: 'A céldátum megadása kötelező.',
  targetTime: 'A célidő 1 perc és 23:59:59 között lehet, óó:pp:mm formátumban.',
};

/**
 * Üres űrlap új célhoz.
 */
export function emptyGoalForm(): GoalFormValues {
  return { type: null, distanceKm: '', targetDate: null, targetTime: '' };
}

/**
 * Egy meglévő cél űrlap-értékei (módosításhoz); fix típusnál a táv a típusból adódik, ezért üres.
 */
export function goalToFormValues(goal: GoalResponse): GoalFormValues {
  return {
    type: goal.type,
    distanceKm: goal.type === 'custom' ? goal.distanceKm : '',
    targetDate: goal.targetDate,
    targetTime: goal.targetTime ?? '',
  };
}

/**
 * Az űrlapból összeállított kérés validálás előtt; a célidő óó:pp:mm alakra igazodik.
 */
function toCandidate(values: GoalFormValues): Record<string, unknown> {
  const seconds = values.targetTime.trim() === '' ? null : parseDuration(values.targetTime);
  return {
    type: values.type ?? undefined,
    distanceKm: values.type === 'custom' && values.distanceKm !== '' ? Number(values.distanceKm) : null,
    targetDate: values.targetDate ?? undefined,
    targetTime: values.targetTime.trim() === '' ? null : seconds === null ? values.targetTime.trim() : formatSeconds(seconds),
  };
}

/**
 * A cél ellenőrzése a közös sémával, plusz a szerveroldali szabályokkal: egyéni célnál kötelező a táv,
 * és a céldátum nem lehet múltbeli.
 */
export function validateGoal(values: GoalFormValues, today: string): Record<string, string> {
  const errors = schemaErrors(zGoalRequest, toCandidate(values), fieldMessages);

  if (values.type === 'custom' && values.distanceKm === '') {
    errors.distanceKm ??= 'Egyéni célnál a táv megadása kötelező.';
  }

  if (values.targetDate && values.targetDate < today) {
    errors.targetDate ??= 'A céldátum nem lehet múltbeli.';
  }

  return errors;
}

/**
 * Az ellenőrzött űrlapból a szervernek küldendő kérés.
 */
export function toGoalRequest(values: GoalFormValues): GoalRequest {
  return zGoalRequest.parse(toCandidate(values));
}
