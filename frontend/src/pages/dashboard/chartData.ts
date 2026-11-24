import type { DonutChartCell } from '@mantine/charts';
import dayjs from 'dayjs';
import type { IntensityDistribution, WeeklyStats } from '../../api/analytics/types.gen';

/** Egy hét a diagramokhoz: a hét kezdőnapja (hh. nn.) és az értékek. */
export type WeeklyChartPoint = {
  week: string;
  km: number;
  load: number;
  acwr: number | null;
  risk: number;
  planned: number;
  completed: number;
  missed: number;
};

/**
 * A 12 hét adatai a diagramok formájában (a hét hétfőjének dátumával címkézve).
 */
export function toWeeklyChartData(weeks: WeeklyStats[]): WeeklyChartPoint[] {
  return weeks.map((week) => ({
    week: dayjs(week.weekStart).format('MM. DD.'),
    km: week.distanceKm,
    load: week.load,
    acwr: week.acwr ?? null,
    risk: week.riskScore,
    planned: week.plannedCount,
    completed: week.completedCount,
    missed: week.missedCount,
  }));
}

/**
 * Az intenzitáseloszlás a fánkdiagramhoz (perc): könnyű (RPE 1–4), közepes (5–6), intenzív (7–10).
 */
export function intensityCells(intensity: IntensityDistribution): DonutChartCell[] {
  return [
    { name: 'Könnyű', value: intensity.easyMinutes, color: 'green.6' },
    { name: 'Közepes', value: intensity.moderateMinutes, color: 'yellow.6' },
    { name: 'Intenzív', value: intensity.intenseMinutes, color: 'red.6' },
  ];
}
