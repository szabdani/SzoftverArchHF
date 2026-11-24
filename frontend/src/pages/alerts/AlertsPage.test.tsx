import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import * as sdk from '../../api/analytics/sdk.gen';
import { renderWithProviders } from '../../test/render';
import { AlertsPage } from './AlertsPage';

vi.mock('../../api/analytics/sdk.gen', () => ({
  getApiAnalyticsAlerts: vi.fn(),
  postApiAnalyticsAlertsByIdAcknowledge: vi.fn(),
}));

/** A figyelmeztetések oldala. */
describe('AlertsPage', () => {
  it('alapból a nyugtázatlanokat kéri, a kapcsolóval a nyugtázottakat is', async () => {
    vi.mocked(sdk.getApiAnalyticsAlerts).mockResolvedValue({
      data: [{ id: 'a1', runnerId: 'r', rule: 'noRestDay', severity: 'info', message: 'Pihenőnap hiánya: 7 egymást követő edzésnap', createdAt: '2026-11-25T08:00:00Z', acknowledgedAt: '2026-11-25T09:00:00Z' }],
    } as never);
    renderWithProviders(<AlertsPage />);

    expect(await screen.findByText('Pihenőnap hiánya: 7 egymást követő edzésnap')).toBeInTheDocument();
    expect(sdk.getApiAnalyticsAlerts).toHaveBeenCalledWith(expect.objectContaining({ query: { includeAcknowledged: false } }));
    expect(screen.queryByRole('button', { name: 'Nyugtázom' })).not.toBeInTheDocument();

    await userEvent.click(screen.getByRole('switch', { name: 'Nyugtázottak is' }));
    await waitFor(() =>
      expect(sdk.getApiAnalyticsAlerts).toHaveBeenCalledWith(expect.objectContaining({ query: { includeAcknowledged: true } })),
    );
  });
});
