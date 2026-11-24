import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Route, Routes } from 'react-router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import * as analytics from '../../api/analytics/sdk.gen';
import * as identity from '../../api/identity/sdk.gen';
import * as training from '../../api/training/sdk.gen';
import { renderWithProviders } from '../../test/render';
import { CoachRunnerPage } from './CoachRunnerPage';

vi.mock('../../api/identity/sdk.gen', () => ({
  getApiIdentityLinks: vi.fn(),
}));
vi.mock('../../api/analytics/sdk.gen', () => ({
  getApiAnalyticsDashboard: vi.fn(),
  getApiAnalyticsRisk: vi.fn(),
  getApiAnalyticsRecommendation: vi.fn(),
  getApiAnalyticsAlerts: vi.fn(),
  postApiAnalyticsAlertsByIdAcknowledge: vi.fn(),
}));
vi.mock('../../api/training/sdk.gen', () => ({
  getApiTrainingWorkouts: vi.fn(),
}));

const runnerId = 'd0000000-0000-4000-8000-000000000001';

/** Az edző nézete a futó oldalán, a futó azonosítójával az útvonalban. */
function renderRunnerPage() {
  renderWithProviders(
    <Routes>
      <Route path="/coach/runners/:runnerId" element={<CoachRunnerPage />} />
    </Routes>,
    { route: `/coach/runners/${runnerId}` },
  );
}

/** Egy hozzárendelt futó az edző szemével, mockolt API-val. */
describe('CoachRunnerPage', () => {
  beforeEach(() => {
    vi.mocked(identity.getApiIdentityLinks).mockResolvedValue({
      data: [{ id: 'l1', coachId: 'c', coachEmail: 'coach@futoedzes.local', runnerEmail: 'anna@pelda.hu', runnerId, status: 'accepted', createdAt: '', updatedAt: '' }],
    } as never);
    // A dashboard válaszai itt nem számítanak, csak az, hogy a futó azonosítójával kérdez le.
    for (const fn of [analytics.getApiAnalyticsDashboard, analytics.getApiAnalyticsRisk, analytics.getApiAnalyticsRecommendation, analytics.getApiAnalyticsAlerts]) {
      vi.mocked(fn).mockReturnValue(new Promise(() => {}) as never);
    }
    vi.mocked(training.getApiTrainingWorkouts).mockResolvedValue({
      data: [{ id: 'w1', runnerId, date: '2026-11-20', type: 'easy', distanceKm: 8, duration: '00:45:00', paceSecondsPerKm: 338, rpe: 4, load: 180, createdAt: '', updatedAt: '' }],
    } as never);
  });

  it('a futó dashboardját és edzésnaplóját mutatja, a naplót szerkesztő gombok nélkül', async () => {
    renderRunnerPage();

    expect(await screen.findByRole('heading', { name: 'anna@pelda.hu' })).toBeInTheDocument();
    await waitFor(() => expect(analytics.getApiAnalyticsDashboard).toHaveBeenCalledWith(expect.objectContaining({ query: { period: 'week', runnerId } })));

    await userEvent.click(screen.getByRole('tab', { name: 'Edzésnapló' }));
    expect(await screen.findByText('8 km')).toBeInTheDocument();
    expect(training.getApiTrainingWorkouts).toHaveBeenCalledWith(expect.objectContaining({ query: expect.objectContaining({ runnerId }) }));
    expect(screen.queryByRole('button', { name: 'Új edzés' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Módosítás' })).not.toBeInTheDocument();
  });

  it('nem hozzárendelt futónál nem kérdezi le az adatokat', async () => {
    vi.mocked(identity.getApiIdentityLinks).mockResolvedValue({ data: [] } as never);
    renderRunnerPage();

    expect(await screen.findByText('Ez a futó nincs hozzád rendelve, ezért az adatai nem láthatók.')).toBeInTheDocument();
    expect(analytics.getApiAnalyticsDashboard).not.toHaveBeenCalled();
  });
});
