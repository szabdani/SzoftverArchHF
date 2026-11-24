import { describe, expect, it } from 'vitest';
import { calculateLoad, calculatePace, formatKm, formatPace, formatSeconds, parseDuration } from './format';

/** A formázók és a számítások (a szerverrel azonos képletek). */
describe('format', () => {
  it('magyar formában írja a távot', () => {
    expect(formatKm(12.5)).toBe('12,5 km');
    expect(formatKm(8)).toBe('8 km');
  });

  it('óó:pp:mm formában írja az időt, 24 óra fölött is', () => {
    expect(formatSeconds(2730)).toBe('00:45:30');
    expect(formatSeconds(90_061)).toBe('25:01:01');
  });

  it('perc:mp/km formában írja a tempót, hiánynál kötőjel', () => {
    expect(formatPace(330)).toBe('5:30 /km');
    expect(formatPace(305)).toBe('5:05 /km');
    expect(formatPace(null)).toBe('–');
  });

  it('értelmezi az óó:pp:mm időt', () => {
    expect(parseDuration('01:05:30')).toBe(3930);
    expect(parseDuration(' 0:45:00 ')).toBe(2700);
    expect(parseDuration('45:00')).toBeNull();
    expect(parseDuration('00:60:00')).toBeNull();
  });

  it('a szerverrel azonosan számolja az sRPE-t és a tempót', () => {
    expect(calculateLoad(6, 2730)).toBe(273);
    expect(calculateLoad(5, 61)).toBe(5.1);
    expect(calculatePace(1500, 5)).toBe(300);
    expect(calculatePace(1500, 0)).toBeNull();
  });
});
