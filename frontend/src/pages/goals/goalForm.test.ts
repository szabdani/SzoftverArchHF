import { describe, expect, it } from 'vitest';
import { toGoalRequest, validateGoal } from './goalForm';

const today = '2026-11-23';

/** A cél-űrlap validációja (közös séma + szerveroldali szabályok). */
describe('validateGoal', () => {
  it('fix típusú célnál a táv nem kell, a célidő óó:pp:mm alakra igazodik', () => {
    const values = { type: 'halfMarathon' as const, distanceKm: '', targetDate: '2027-04-12', targetTime: '1:45:00' };

    expect(validateGoal(values, today)).toEqual({});
    expect(toGoalRequest(values)).toEqual({ type: 'halfMarathon', distanceKm: null, targetDate: '2027-04-12', targetTime: '01:45:00' });
  });

  it('egyéni célnál kötelező a táv, és a séma határát is jelzi', () => {
    expect(validateGoal({ type: 'custom', distanceKm: '', targetDate: '2027-01-01', targetTime: '' }, today)).toEqual({
      distanceKm: 'Egyéni célnál a táv megadása kötelező.',
    });
    expect(validateGoal({ type: 'custom', distanceKm: 501, targetDate: '2027-01-01', targetTime: '' }, today)).toHaveProperty('distanceKm');
  });

  it('a múltbeli céldátumot és a 24 órás célidőt elutasítja', () => {
    expect(validateGoal({ type: 'tenK', distanceKm: '', targetDate: '2026-11-22', targetTime: '' }, today)).toEqual({
      targetDate: 'A céldátum nem lehet múltbeli.',
    });
    expect(validateGoal({ type: 'tenK', distanceKm: '', targetDate: today, targetTime: '24:00:00' }, today)).toHaveProperty('targetTime');
  });
});
