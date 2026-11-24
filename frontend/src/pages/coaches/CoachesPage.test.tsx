import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import * as sdk from '../../api/identity/sdk.gen';
import { renderWithProviders } from '../../test/render';
import { CoachesPage } from './CoachesPage';

vi.mock('../../api/identity/sdk.gen', () => ({
  getApiIdentityInvitationsReceived: vi.fn(),
  getApiIdentityLinks: vi.fn(),
  postApiIdentityInvitationsByIdAccept: vi.fn(),
  postApiIdentityInvitationsByIdDecline: vi.fn(),
  deleteApiIdentityLinksById: vi.fn(),
}));

/** A futó edzői kapcsolatai. */
describe('CoachesPage', () => {
  it('a meghívó elfogadható, a meglévő kapcsolat megerősítés után megszüntethető', async () => {
    vi.mocked(sdk.getApiIdentityInvitationsReceived).mockResolvedValue({
      data: [{ id: 'i1', coachId: 'c', coachEmail: 'coach@futoedzes.local', runnerEmail: 'runner@futoedzes.local', status: 'pending', createdAt: '2026-11-22T10:00:00Z', updatedAt: '' }],
    } as never);
    vi.mocked(sdk.getApiIdentityLinks).mockResolvedValue({
      data: [{ id: 'l1', coachId: 'c2', coachEmail: 'masik@futoedzes.local', runnerEmail: 'runner@futoedzes.local', runnerId: 'r', status: 'accepted', createdAt: '', updatedAt: '2026-11-01T10:00:00Z' }],
    } as never);
    vi.mocked(sdk.postApiIdentityInvitationsByIdAccept).mockResolvedValue({ data: {} } as never);
    vi.mocked(sdk.deleteApiIdentityLinksById).mockResolvedValue({ data: undefined } as never);
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    renderWithProviders(<CoachesPage />);

    await userEvent.click(await screen.findByRole('button', { name: 'Elfogadom' }));
    await waitFor(() => expect(sdk.postApiIdentityInvitationsByIdAccept).toHaveBeenCalledWith(expect.objectContaining({ path: { id: 'i1' } })));

    await userEvent.click(screen.getByRole('button', { name: 'Kapcsolat megszüntetése' }));
    await waitFor(() => expect(sdk.deleteApiIdentityLinksById).toHaveBeenCalledWith(expect.objectContaining({ path: { id: 'l1' } })));
  });
});
