import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Route, Routes } from 'react-router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import * as analytics from '../../api/analytics/sdk.gen';
import * as identity from '../../api/identity/sdk.gen';
import { renderWithProviders } from '../../test/render';
import { CoachOverviewPage } from './CoachOverviewPage';

vi.mock('../../api/identity/sdk.gen', () => ({
  getApiIdentityLinks: vi.fn(),
  getApiIdentityInvitationsSent: vi.fn(),
  postApiIdentityInvitations: vi.fn(),
}));
vi.mock('../../api/analytics/sdk.gen', () => ({
  getApiAnalyticsCoachRunners: vi.fn(),
}));

/**
 * A táblázat sorainak első cellái (a futók e-mail címei) a fejléc nélkül.
 */
function rowEmails(): string[] {
  return screen.getAllByRole('row').slice(1).map((row) => within(row).getAllByRole('cell')[0].textContent ?? '');
}

/** Az edző áttekintő oldala mockolt API-val. */
describe('CoachOverviewPage', () => {
  beforeEach(() => {
    vi.mocked(identity.getApiIdentityLinks).mockResolvedValue({
      data: [
        { id: 'l1', coachId: 'c', coachEmail: 'coach@futoedzes.local', runnerEmail: 'anna@pelda.hu', runnerId: 'r1', status: 'accepted', createdAt: '', updatedAt: '' },
        { id: 'l2', coachId: 'c', coachEmail: 'coach@futoedzes.local', runnerEmail: 'bela@pelda.hu', runnerId: 'r2', status: 'accepted', createdAt: '', updatedAt: '' },
      ],
    } as never);
    vi.mocked(analytics.getApiAnalyticsCoachRunners).mockResolvedValue({
      data: [
        { runnerId: 'r1', lastWorkoutDate: '2026-11-20', weeklyKm: 30, riskScore: 20, riskCategory: 'low' },
        { runnerId: 'r2', lastWorkoutDate: '2026-11-22', weeklyKm: 55, riskScore: 70, riskCategory: 'high' },
      ],
    } as never);
    vi.mocked(identity.getApiIdentityInvitationsSent).mockResolvedValue({
      data: [{ id: 'i1', coachId: 'c', coachEmail: 'coach@futoedzes.local', runnerEmail: 'cecil@pelda.hu', status: 'pending', createdAt: '2026-11-23T10:00:00Z', updatedAt: '' }],
    } as never);
  });

  it('a futókat kockázat szerint rendezi, a sorra kattintva a futó oldalára visz', async () => {
    renderWithProviders(
      <Routes>
        <Route path="/coach" element={<CoachOverviewPage />} />
        <Route path="/coach/runners/:runnerId" element={<p>Futó oldala</p>} />
      </Routes>,
      { route: '/coach' },
    );

    expect(await screen.findByText('70 · Magas')).toBeInTheDocument();
    expect(rowEmails()).toEqual(['bela@pelda.hu', 'anna@pelda.hu']);
    expect(screen.getByText(/cecil@pelda.hu/)).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: 'Kockázat' }));
    expect(rowEmails()).toEqual(['anna@pelda.hu', 'bela@pelda.hu']);

    await userEvent.click(screen.getByText('bela@pelda.hu'));
    expect(await screen.findByText('Futó oldala')).toBeInTheDocument();
  });

  it('érvénytelen e-mail címre nem küld meghívót, érvényesre igen', async () => {
    vi.mocked(identity.postApiIdentityInvitations).mockResolvedValue({ data: {} } as never);
    renderWithProviders(<CoachOverviewPage />);

    const email = screen.getByRole('textbox', { name: 'A futó e-mail címe' });
    await userEvent.type(email, 'nem-email');
    await userEvent.click(screen.getByRole('button', { name: 'Meghívás' }));
    expect(await screen.findByText('Adj meg egy érvényes e-mail címet.')).toBeInTheDocument();
    expect(identity.postApiIdentityInvitations).not.toHaveBeenCalled();

    await userEvent.clear(email);
    await userEvent.type(email, ' dora@pelda.hu ');
    await userEvent.click(screen.getByRole('button', { name: 'Meghívás' }));
    await waitFor(() => expect(identity.postApiIdentityInvitations).toHaveBeenCalledWith(expect.objectContaining({ body: { email: 'dora@pelda.hu' } })));
  });
});
