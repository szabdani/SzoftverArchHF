import { describe, expect, it } from 'vitest';
import { intensityCells, toWeeklyChartData } from './chartData';

/** A dashboard diagramadatai. */
describe('chartData', () => {
  it('a heteket a hétfő dátumával címkézi, az ACWR hiányát null-lal jelzi', () => {
    const data = toWeeklyChartData([
      { weekStart: '2026-11-16', distanceKm: 32.5, load: 1400, acwr: 1.62, riskScore: 60, workoutCount: 5, plannedCount: 5, completedCount: 4, missedCount: 1 },
      { weekStart: '2026-11-23', distanceKm: 0, load: 0, riskScore: 0, workoutCount: 0, plannedCount: 0, completedCount: 0, missedCount: 0 },
    ]);

    expect(data).toEqual([
      { week: '11. 16.', km: 32.5, load: 1400, acwr: 1.62, risk: 60, planned: 5, completed: 4, missed: 1 },
      { week: '11. 23.', km: 0, load: 0, acwr: null, risk: 0, planned: 0, completed: 0, missed: 0 },
    ]);
  });

  it('az intenzitáseloszlást a három sávra bontja', () => {
    expect(intensityCells({ easyMinutes: 120, moderateMinutes: 45, intenseMinutes: 30 }).map((cell) => [cell.name, cell.value])).toEqual([
      ['Könnyű', 120],
      ['Közepes', 45],
      ['Intenzív', 30],
    ]);
  });
});
