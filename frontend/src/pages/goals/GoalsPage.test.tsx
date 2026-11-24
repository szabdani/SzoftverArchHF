import { screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { ApiError } from '../../api/apiError';
import * as sdk from '../../api/training/sdk.gen';
import { renderWithProviders } from '../../test/render';
import { GoalsPage } from './GoalsPage';

vi.mock('../../api/training/sdk.gen', () => ({
  getApiTrainingGoals: vi.fn(),
  getApiTrainingGoalsActive: vi.fn(),
  postApiTrainingGoals: vi.fn(),
  putApiTrainingGoalsById: vi.fn(),
}));

/** A célok oldala. */
describe('GoalsPage', () => {
  it('aktív cél nélkül (404) jelzi, hogy nincs cél', async () => {
    vi.mocked(sdk.getApiTrainingGoalsActive).mockRejectedValue(new ApiError(404, ''));
    vi.mocked(sdk.getApiTrainingGoals).mockResolvedValue({ data: [] } as never);
    renderWithProviders(<GoalsPage />);

    expect(await screen.findByText(/Nincs aktív cél/)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Új cél' })).toBeInTheDocument();
  });

  it('az aktív célt a hátralévő hetekkel mutatja; csak olvasható nézetben (edző) gombok nélkül', async () => {
    const goal = { id: 'g1', runnerId: 'r', type: 'halfMarathon' as const, distanceKm: 21.0975, targetDate: '2027-04-12', targetTime: '01:45:00', isActive: true, createdAt: '' };
    vi.mocked(sdk.getApiTrainingGoalsActive).mockResolvedValue({ data: { goal, weeksRemaining: 20, recentWorkouts: [] } } as never);
    vi.mocked(sdk.getApiTrainingGoals).mockResolvedValue({ data: [goal, { ...goal, id: 'g0', type: 'tenK', distanceKm: 10, isActive: false, archivedAt: '2026-11-01T10:00:00Z' }] } as never);
    renderWithProviders(<GoalsPage runnerId="r" readOnly />);

    expect(await screen.findByText('Hátralévő hetek: 20')).toBeInTheDocument();
    expect(screen.getByText(/célidő: 01:45:00/)).toBeInTheDocument();
    expect(await screen.findByText('Archiválva: 2026. 11. 01.')).toBeInTheDocument();
    expect(sdk.getApiTrainingGoalsActive).toHaveBeenCalledWith(expect.objectContaining({ query: { runnerId: 'r' } }));
    expect(screen.queryByRole('button', { name: 'Új cél' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Módosítás' })).not.toBeInTheDocument();
  });
});
