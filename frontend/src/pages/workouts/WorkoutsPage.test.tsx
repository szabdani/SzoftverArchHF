import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '../../api/apiError';
import * as sdk from '../../api/training/sdk.gen';
import type { WorkoutResponse } from '../../api/training/types.gen';
import { renderWithProviders } from '../../test/render';
import { WorkoutsPage } from './WorkoutsPage';

vi.mock('../../api/training/sdk.gen', () => ({
  getApiTrainingWorkouts: vi.fn(),
  getApiTrainingPlannedWorkouts: vi.fn(),
  postApiTrainingWorkouts: vi.fn(),
  putApiTrainingWorkoutsById: vi.fn(),
  deleteApiTrainingWorkoutsById: vi.fn(),
}));

const workouts: WorkoutResponse[] = [
  { id: 'w1', runnerId: 'r', date: '2026-11-20', type: 'easy', distanceKm: 8, duration: '00:45:00', paceSecondsPerKm: 338, rpe: 4, load: 180, createdAt: '', updatedAt: '' },
  { id: 'w2', runnerId: 'r', date: '2026-11-22', type: 'long', distanceKm: 20.5, duration: '02:00:00', paceSecondsPerKm: 351, rpe: 5, load: 600, createdAt: '', updatedAt: '' },
];

/**
 * A táblázat sorainak első cellái (dátumok) a fejléc nélkül.
 */
function rowDates(): string[] {
  return screen.getAllByRole('row').slice(1).map((row) => within(row).getAllByRole('cell')[0].textContent ?? '');
}

/** Az edzésnapló oldal mockolt API-val. */
describe('WorkoutsPage', () => {
  beforeEach(() => {
    vi.mocked(sdk.getApiTrainingWorkouts).mockResolvedValue({ data: workouts } as never);
  });

  it('listázza az edzéseket, alapból a legújabb elöl, és táv szerint rendezhető', async () => {
    renderWithProviders(<WorkoutsPage />);

    expect(await screen.findByText('20,5 km')).toBeInTheDocument();
    expect(rowDates()).toEqual(['2026. 11. 22.', '2026. 11. 20.']);

    // Az első kattintás csökkenő sorrend, a második növekvő.
    await userEvent.click(screen.getByRole('button', { name: 'Táv' }));
    await userEvent.click(screen.getByRole('button', { name: 'Táv' }));
    expect(rowDates()).toEqual(['2026. 11. 20.', '2026. 11. 22.']);
  });

  it('szerkesztéskor a módosított edzést küldi, a szerver mezőhibáját a mezőnél mutatja', async () => {
    vi.mocked(sdk.putApiTrainingWorkoutsById).mockRejectedValue(
      new ApiError(400, { errors: { date: ['A dátum nem lehet jövőbeli.'] } }),
    );
    renderWithProviders(<WorkoutsPage />);

    await userEvent.click((await screen.findAllByRole('button', { name: 'Módosítás' }))[1]);
    const distance = await screen.findByRole('textbox', { name: /Táv/ });
    await userEvent.clear(distance);
    await userEvent.type(distance, '9,5');
    await userEvent.click(screen.getByRole('button', { name: 'Mentés' }));

    await waitFor(() => expect(sdk.putApiTrainingWorkoutsById).toHaveBeenCalled());
    expect(vi.mocked(sdk.putApiTrainingWorkoutsById).mock.calls[0][0]).toMatchObject({
      path: { id: 'w1' },
      body: { date: '2026-11-20', type: 'easy', distanceKm: 9.5, duration: '00:45:00', rpe: 4 },
    });
    expect(await screen.findByText('A dátum nem lehet jövőbeli.')).toBeInTheDocument();
  });
});
