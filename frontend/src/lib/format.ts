import type { GoalType, PlannedWorkoutStatus, WorkoutType } from '../api/training/types.gen';

const number = new Intl.NumberFormat('hu-HU', { maximumFractionDigits: 2 });

/** Az edzéstípusok magyar neve (spec 3.2). */
export const workoutTypeLabels: Record<WorkoutType, string> = {
  easy: 'Könnyű',
  recovery: 'Regeneráló',
  tempo: 'Tempó',
  interval: 'Intervall',
  long: 'Hosszú',
  race: 'Verseny',
};

/** A céltípusok magyar neve (spec 3.3). */
export const goalTypeLabels: Record<GoalType, string> = {
  fiveK: '5 km',
  tenK: '10 km',
  halfMarathon: 'Félmaraton',
  marathon: 'Maraton',
  custom: 'Egyéni táv',
};

/** A tervezett edzés állapotai (spec 3.4): magyar név és Mantine szín. */
export const plannedStatuses: Record<PlannedWorkoutStatus, { label: string; color: string }> = {
  planned: { label: 'Tervezett', color: 'blue' },
  completed: { label: 'Teljesített', color: 'teal' },
  missed: { label: 'Kihagyott', color: 'red' },
};

/** Kockázati kategóriák: magyar név és Mantine szín. */
export const riskCategories: Record<'low' | 'medium' | 'high', { label: string; color: string }> = {
  low: { label: 'Alacsony', color: 'green' },
  medium: { label: 'Közepes', color: 'yellow' },
  high: { label: 'Magas', color: 'red' },
};

/** A figyelmeztetések súlyossága: magyar név és Mantine szín. */
export const alertSeverities: Record<'info' | 'warning' | 'critical', { label: string; color: string }> = {
  info: { label: 'Információ', color: 'blue' },
  warning: { label: 'Figyelmeztetés', color: 'orange' },
  critical: { label: 'Kritikus', color: 'red' },
};

/**
 * Dátum magyar formában (pl. „2026. 11. 23.”).
 */
export function formatDate(date: string): string {
  const [year, month, day] = date.slice(0, 10).split('-');
  return `${year}. ${month}. ${day}.`;
}

/**
 * Táv kilométerben, magyar formában (pl. „12,5 km”).
 */
export function formatKm(km: number): string {
  return `${number.format(km)} km`;
}

/**
 * Másodpercek óó:pp:mm formában; az óra 24 fölé is mehet (havi összesítő).
 */
export function formatSeconds(totalSeconds: number): string {
  const hours = Math.floor(totalSeconds / 3600);
  const minutes = Math.floor((totalSeconds % 3600) / 60);
  const seconds = totalSeconds % 60;
  return [hours, minutes, seconds].map((part) => String(part).padStart(2, '0')).join(':');
}

/**
 * Tempó perc:mp/km formában (pl. „5:30 /km”); hiányzó értéknél „–”.
 */
export function formatPace(secondsPerKm: number | null | undefined): string {
  if (!secondsPerKm) {
    return '–';
  }

  const minutes = Math.floor(secondsPerKm / 60);
  const seconds = Math.round(secondsPerKm % 60);
  return `${minutes}:${String(seconds).padStart(2, '0')} /km`;
}

/**
 * óó:pp:mm szöveg másodpercekre; érvénytelen formánál null.
 */
export function parseDuration(value: string): number | null {
  const match = /^(\d{1,2}):([0-5]\d):([0-5]\d)$/.exec(value.trim());
  return match ? Number(match[1]) * 3600 + Number(match[2]) * 60 + Number(match[3]) : null;
}

/**
 * Edzésterhelés (sRPE): RPE × időtartam percben, egy tizedesre kerekítve (ugyanúgy, mint a szerveren).
 */
export function calculateLoad(rpe: number, durationSeconds: number): number {
  return Math.round(((rpe * durationSeconds) / 60) * 10) / 10;
}

/**
 * Tempó másodperc/km-ben (időtartam / táv), egészre kerekítve; nulla távnál null.
 */
export function calculatePace(durationSeconds: number, distanceKm: number): number | null {
  return distanceKm > 0 ? Math.round(durationSeconds / distanceKm) : null;
}
