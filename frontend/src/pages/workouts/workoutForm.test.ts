import { describe, expect, it } from 'vitest';
import { emptyWorkoutForm, toWorkoutRequest, validateWorkout, type WorkoutFormValues } from './workoutForm';

const today = '2026-11-23';

/** Egy minden mezőjében érvényes űrlap, amiből a tesztek egy-egy mezőt rontanak el. */
const valid: WorkoutFormValues = {
  date: '2026-11-22',
  type: 'easy',
  distanceKm: 8.5,
  duration: '0:48:30',
  avgHeartRate: '',
  elevationGainM: 60,
  rpe: 4,
  note: '  Könnyű kör  ',
  plannedWorkoutId: null,
};

/** Az edzés-űrlap validációja a szerverrel közös (generált) sémával. */
describe('validateWorkout', () => {
  it('az érvényes űrlapot elfogadja, és szerverkéréssé alakítja', () => {
    expect(validateWorkout(valid, today)).toEqual({});
    expect(toWorkoutRequest(valid)).toEqual({
      date: '2026-11-22',
      type: 'easy',
      distanceKm: 8.5,
      duration: '00:48:30',
      avgHeartRate: null,
      elevationGainM: 60,
      rpe: 4,
      note: 'Könnyű kör',
      plannedWorkoutId: null,
    });
  });

  it('a séma határait mezőnként magyar üzenettel jelzi', () => {
    expect(validateWorkout({ ...valid, duration: '10:00:01' }, today)).toEqual({
      duration: 'Az időtartam 1 perc és 10 óra között lehet, óó:pp:mm formátumban.',
    });
    expect(validateWorkout({ ...valid, distanceKm: 100.5 }, today)).toHaveProperty('distanceKm');
    expect(validateWorkout({ ...valid, avgHeartRate: 39 }, today)).toHaveProperty('avgHeartRate');
    expect(validateWorkout({ ...valid, rpe: 11 }, today)).toHaveProperty('rpe');
    expect(validateWorkout({ ...valid, note: 'a'.repeat(501) }, today)).toHaveProperty('note');
  });

  it('a jövőbeli dátumot elutasítja (a mai még jó)', () => {
    expect(validateWorkout({ ...valid, date: '2026-11-24' }, today)).toEqual({ date: 'A dátum nem lehet jövőbeli.' });
    expect(validateWorkout({ ...valid, date: today }, today)).toEqual({});
  });

  it('az üres űrlapnál a kötelező mezőket jelzi', () => {
    const errors = validateWorkout({ ...emptyWorkoutForm(today), date: null }, today);

    expect(Object.keys(errors).sort()).toEqual(['date', 'distanceKm', 'duration', 'rpe', 'type']);
  });
});
