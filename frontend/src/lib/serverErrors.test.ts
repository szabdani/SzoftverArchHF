import { describe, expect, it, vi } from 'vitest';
import { ApiError, errorMessage } from '../api/apiError';
import { applyServerErrors } from './serverErrors';

/** A szerver hibáinak megjelenítése. */
describe('applyServerErrors', () => {
  it('a 400-as validációs hibákat mezőnként az űrlapra teszi', () => {
    const form = { setErrors: vi.fn() };
    const error = new ApiError(400, { title: 'One or more validation errors occurred.', errors: { date: ['A dátum nem lehet jövőbeli.'], rpe: ['a', 'b'] } });

    expect(applyServerErrors(form, error)).toBe(true);
    expect(form.setErrors).toHaveBeenCalledWith({ date: 'A dátum nem lehet jövőbeli.', rpe: 'a b' });
  });

  it('más hibát nem tesz az űrlapra, hanem magyar üzenetet ad róla', () => {
    const form = { setErrors: vi.fn() };

    expect(applyServerErrors(form, new ApiError(403, ''))).toBe(false);
    expect(form.setErrors).not.toHaveBeenCalled();
    expect(errorMessage(new ApiError(403, ''))).toBe('Ehhez nincs jogosultságod.');
    expect(errorMessage(new ApiError(409, { title: 'Erre a címre már van függő meghívásod.' }))).toBe('Erre a címre már van függő meghívásod.');
    expect(errorMessage(new Error('hálózat'))).toBe('Váratlan hiba történt.');
  });
});
