import { describe, expect, it } from 'vitest';
import type { PlanResponse } from '../../api/training/types.gen';
import { toPlannedWorkoutRequest, toPlanRequest, validatePlan, validatePlannedWorkout, type PlannedWorkoutFormValues } from './planForms';

const plan: PlanResponse = { id: 'p1', runnerId: 'r', createdById: 'r', name: 'Alapozás', startDate: '2026-11-23', endDate: '2026-12-20', createdAt: '', workouts: [] };

const planned: PlannedWorkoutFormValues = {
  planId: 'p1',
  date: '2026-11-25',
  type: 'interval',
  plannedDistanceKm: '',
  plannedDuration: '0:50:00',
  targetRpe: 8,
  note: '',
};

/** A terv és a tervezett edzés űrlapjának szabályai. */
describe('planForms', () => {
  it('a terv: kötelező név, a záródátum nem korábbi, legfeljebb egy év; az edző a futónak küldi', () => {
    expect(validatePlan({ name: ' ', startDate: '2026-11-23', endDate: '2026-11-22', linkActiveGoal: true })).toEqual({
      name: 'A terv neve kötelező (legfeljebb 100 karakter).',
      endDate: 'A záródátum nem lehet korábbi a kezdődátumnál.',
    });
    expect(validatePlan({ name: 'Év', startDate: '2026-01-01', endDate: '2027-01-03', linkActiveGoal: false })).toHaveProperty('endDate');
    expect(toPlanRequest({ name: ' Alapozás ', startDate: '2026-11-23', endDate: '2026-12-20', linkActiveGoal: true }, 'd0000000-0000-4000-8000-000000000001')).toEqual({
      runnerId: 'd0000000-0000-4000-8000-000000000001',
      name: 'Alapozás',
      startDate: '2026-11-23',
      endDate: '2026-12-20',
      linkActiveGoal: true,
    });
  });

  it('a tervezett edzés: érvényes, az időtartam igazodik', () => {
    expect(validatePlannedWorkout(planned, plan)).toEqual({});
    expect(toPlannedWorkoutRequest(planned)).toEqual({
      date: '2026-11-25',
      type: 'interval',
      plannedDistanceKm: null,
      plannedDuration: '00:50:00',
      targetRpe: 8,
      note: null,
    });
  });

  it('a tervezett edzés: kell terv, a dátum a terv időszakában, és táv vagy időtartam', () => {
    expect(validatePlannedWorkout({ ...planned, planId: null }, undefined)).toHaveProperty('planId');
    expect(validatePlannedWorkout({ ...planned, date: '2026-12-21' }, plan)).toEqual({ date: 'A dátumnak a terv időszakába kell esnie.' });
    expect(validatePlannedWorkout({ ...planned, plannedDuration: '' }, plan)).toEqual({
      plannedDistanceKm: 'A tervezett táv vagy időtartam közül legalább az egyiket meg kell adni.',
    });
  });
});
