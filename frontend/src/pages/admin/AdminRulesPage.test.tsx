import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import * as sdk from '../../api/analytics/sdk.gen';
import { renderWithProviders } from '../../test/render';
import { AdminRulesPage } from './AdminRulesPage';

vi.mock('../../api/analytics/sdk.gen', () => ({
  getApiAnalyticsAdminRules: vi.fn(),
  putApiAnalyticsAdminRules: vi.fn(),
}));

/** A szabálybeállítások oldala mockolt API-val. */
describe('AdminRulesPage', () => {
  it('a hibás küszöböt nem menti, a javítottat és a kikapcsolt szabályt igen', async () => {
    vi.mocked(sdk.getApiAnalyticsAdminRules).mockResolvedValue({
      data: {
        acwrCritical: 1.5, acwrElevated: 1.3, weeklyKmIncreasePercent: 10, intenseRpe: 7, intenseDaysPerWeek: 3, missedWorkoutsPerWeek: 2,
        acwrEnabled: true, weeklyKmIncreaseEnabled: true, intenseDaysEnabled: true, noRestDayEnabled: true, missedWorkoutsEnabled: true,
      },
    } as never);
    vi.mocked(sdk.putApiAnalyticsAdminRules).mockResolvedValue({ data: {} } as never);
    renderWithProviders(<AdminRulesPage />);

    const critical = await screen.findByRole('textbox', { name: 'Kritikus e fölött' });
    await userEvent.clear(critical);
    await userEvent.type(critical, '1,2');
    await userEvent.click(screen.getByRole('button', { name: 'Mentés' }));
    expect(await screen.findByText('Az emelkedő ACWR küszöbnek kisebbnek kell lennie a kritikusnál.')).toBeInTheDocument();
    expect(sdk.putApiAnalyticsAdminRules).not.toHaveBeenCalled();

    await userEvent.clear(critical);
    await userEvent.type(critical, '1,8');
    // A kikapcsolt szabály küszöbe nem szerkeszthető.
    await userEvent.click(screen.getByRole('switch', { name: /Kihagyott edzések/ }));
    expect(screen.getByRole('textbox', { name: 'Ennyi kihagyott edzéstől' })).toBeDisabled();
    await userEvent.click(screen.getByRole('button', { name: 'Mentés' }));

    await waitFor(() =>
      expect(sdk.putApiAnalyticsAdminRules).toHaveBeenCalledWith(expect.objectContaining({ body: expect.objectContaining({ acwrCritical: 1.8, missedWorkoutsEnabled: false }) })),
    );
  });
});
