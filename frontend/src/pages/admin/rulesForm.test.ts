import { describe, expect, it } from 'vitest';
import type { RuleSettingsDto } from '../../api/analytics/types.gen';
import { validateRules } from './rulesForm';

/** A spec alapértelmezései, a szerver időbélyegével (eltolással, ahogy a .NET küldi). */
const defaults: RuleSettingsDto = {
  acwrCritical: 1.5,
  acwrElevated: 1.3,
  weeklyKmIncreasePercent: 10,
  intenseRpe: 7,
  intenseDaysPerWeek: 3,
  missedWorkoutsPerWeek: 2,
  acwrEnabled: true,
  weeklyKmIncreaseEnabled: true,
  intenseDaysEnabled: true,
  noRestDayEnabled: true,
  missedWorkoutsEnabled: true,
  updatedAt: '2026-11-20T10:00:00.123456+00:00',
};

/** A szabálybeállítások ellenőrzése. */
describe('validateRules', () => {
  it('az alapértelmezések érvényesek', () => {
    expect(validateRules(defaults)).toEqual({});
  });

  it('a tartományokat a közös sémából veszi', () => {
    expect(validateRules({ ...defaults, intenseRpe: 11, weeklyKmIncreasePercent: 12.5 })).toEqual({
      intenseRpe: 'Az RPE 1 és 10 közötti egész szám lehet.',
      weeklyKmIncreasePercent: 'A növekedés 1 és 100% között lehet.',
    });
  });

  it('az emelkedő ACWR-sávnak a kritikus alatt kell kezdődnie', () => {
    expect(validateRules({ ...defaults, acwrElevated: 1.5 })).toEqual({
      acwrElevated: 'Az emelkedő ACWR küszöbnek kisebbnek kell lennie a kritikusnál.',
    });
  });
});
