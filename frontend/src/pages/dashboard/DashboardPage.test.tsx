import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import * as sdk from '../../api/analytics/sdk.gen';
import type { DashboardResponse } from '../../api/analytics/types.gen';
import { renderWithProviders } from '../../test/render';
import { DashboardPage } from './DashboardPage';

vi.mock('../../api/analytics/sdk.gen', () => ({
  getApiAnalyticsDashboard: vi.fn(),
  getApiAnalyticsRisk: vi.fn(),
  getApiAnalyticsRecommendation: vi.fn(),
  getApiAnalyticsAlerts: vi.fn(),
  postApiAnalyticsAlertsByIdAcknowledge: vi.fn(),
}));

const dashboard: DashboardResponse = {
  period: 'week',
  summary: { from: '2026-11-23', to: '2026-11-25', distanceKm: 42.5, durationSeconds: 14_025, elevationGainM: 320, workoutCount: 4, avgPaceSecondsPerKm: 330, riskScore: 60, riskCategory: 'high' },
  intensity: { easyMinutes: 150, moderateMinutes: 40, intenseMinutes: 45 },
  weeks: [{ weekStart: '2026-11-23', distanceKm: 42.5, load: 1500, acwr: 1.62, riskScore: 60, workoutCount: 4, plannedCount: 4, completedCount: 4, missedCount: 0 }],
};

/** A futó dashboardja mockolt API-val. */
describe('DashboardPage', () => {
  beforeEach(() => {
    vi.mocked(sdk.getApiAnalyticsDashboard).mockResolvedValue({ data: dashboard } as never);
    vi.mocked(sdk.getApiAnalyticsRisk).mockResolvedValue({
      data: { current: { date: '2026-11-25', score: 60, category: 'high', reasons: ['ACWR 1,62 > 1,5', 'Két egymást követő intenzív nap (RPE ≥ 7): 11.24. és 11.25.'], acwr: 1.62, acuteLoad: 1500, chronicLoad: 925, weeklyKm: 42.5, previousWeeklyKm: 30, calculatedAt: '' }, history: [] },
    } as never);
    vi.mocked(sdk.getApiAnalyticsRecommendation).mockResolvedValue({
      data: { type: 'recovery', rpeMin: 2, rpeMax: 3, durationMinutes: 30, reason: 'Magas a kockázati pontszám: ma pihenőnap vagy legfeljebb egy rövid, regeneráló futás javasolt.' },
    } as never);
    vi.mocked(sdk.getApiAnalyticsAlerts).mockResolvedValue({
      data: [{ id: 'a1', runnerId: 'r', rule: 'suddenLoadIncrease', severity: 'critical', message: 'Hirtelen terhelésnövekedés: ACWR 1,62 > 1,5', createdAt: '2026-11-25T08:00:00Z' }],
    } as never);
    vi.mocked(sdk.postApiAnalyticsAlertsByIdAcknowledge).mockResolvedValue({ data: {} } as never);
  });

  it('megmutatja az összesítőt, a kockázatot az indoklásokkal, a javaslatot és a figyelmeztetést', async () => {
    renderWithProviders(<DashboardPage />);

    expect(await screen.findByText('42,5 km')).toBeInTheDocument();
    expect(screen.getByText('03:53:45')).toBeInTheDocument();
    expect(screen.getByText('5:30 /km')).toBeInTheDocument();
    expect(await screen.findByText('ACWR 1,62 > 1,5')).toBeInTheDocument();
    expect(screen.getAllByText('Magas').length).toBeGreaterThan(0);
    expect(await screen.findByText('Regeneráló')).toBeInTheDocument();
    expect(screen.getByText(/RPE 2–3 · kb\. 30 perc/)).toBeInTheDocument();
    expect(await screen.findByText('Hirtelen terhelésnövekedés: ACWR 1,62 > 1,5')).toBeInTheDocument();
  });

  it('a figyelmeztetés nyugtázható, és havi nézetre váltható', async () => {
    renderWithProviders(<DashboardPage />);

    await userEvent.click(await screen.findByRole('button', { name: 'Nyugtázom' }));
    await waitFor(() => expect(sdk.postApiAnalyticsAlertsByIdAcknowledge).toHaveBeenCalledWith(expect.objectContaining({ path: { id: 'a1' } })));

    await userEvent.click(screen.getByText('Havi'));
    await waitFor(() =>
      expect(sdk.getApiAnalyticsDashboard).toHaveBeenCalledWith(expect.objectContaining({ query: { period: 'month' } })),
    );
  });
});
