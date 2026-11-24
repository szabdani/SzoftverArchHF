import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import * as sdk from '../../api/identity/sdk.gen';
import { renderWithProviders } from '../../test/render';
import { AdminUsersPage } from './AdminUsersPage';

vi.mock('react-oidc-context', () => ({ useAuth: () => ({ user: { profile: { sub: 'admin-id' } } }) }));
vi.mock('../../api/identity/sdk.gen', () => ({
  getApiIdentityAdminUsers: vi.fn(),
  putApiIdentityAdminUsersByIdRole: vi.fn(),
  putApiIdentityAdminUsersByIdEnabled: vi.fn(),
  postApiIdentityAdminDemo: vi.fn(),
  deleteApiIdentityAdminDemo: vi.fn(),
}));

/** Az admin felhasználókezelése mockolt API-val. */
describe('AdminUsersPage', () => {
  beforeEach(() => {
    vi.mocked(sdk.getApiIdentityAdminUsers).mockResolvedValue({
      data: [
        { id: 'admin-id', name: 'Admin', email: 'admin@futoedzes.local', role: 'admin', enabled: true },
        { id: 'u2', name: 'Béla', email: 'bela@pelda.hu', role: 'runner', enabled: true },
      ],
    } as never);
    vi.mocked(sdk.putApiIdentityAdminUsersByIdRole).mockResolvedValue({ data: undefined } as never);
    vi.mocked(sdk.putApiIdentityAdminUsersByIdEnabled).mockResolvedValue({ data: undefined } as never);
  });

  it('a más felhasználó szerepköre és aktív állapota módosítható, a sajátja nem', async () => {
    renderWithProviders(<AdminUsersPage />);

    expect(await screen.findByRole('switch', { name: 'Admin aktív' })).toBeDisabled();
    expect(screen.getByRole('combobox', { name: 'Admin szerepköre' })).toBeDisabled();

    await userEvent.click(screen.getByRole('switch', { name: 'Béla aktív' }));
    await waitFor(() => expect(sdk.putApiIdentityAdminUsersByIdEnabled).toHaveBeenCalledWith(expect.objectContaining({ path: { id: 'u2' }, body: { enabled: false } })));

    await userEvent.click(screen.getByRole('combobox', { name: 'Béla szerepköre' }));
    await userEvent.click(await screen.findByRole('option', { name: 'Edző' }));
    await waitFor(() => expect(sdk.putApiIdentityAdminUsersByIdRole).toHaveBeenCalledWith(expect.objectContaining({ path: { id: 'u2' }, body: { role: 'coach' } })));
  });

  it('a keresés a szerveren szűr', async () => {
    renderWithProviders(<AdminUsersPage />);

    await userEvent.type(screen.getByRole('textbox', { name: 'Keresés' }), ' bela ');
    await waitFor(() => expect(sdk.getApiIdentityAdminUsers).toHaveBeenCalledWith(expect.objectContaining({ query: { search: 'bela' } })));
  });

  it('a demóadatok létrehozása után a létrehozott futókat mutatja', async () => {
    vi.mocked(sdk.postApiIdentityAdminDemo).mockResolvedValue({
      data: [{ runnerId: 'd1', email: 'demo.spike@futoedzes.local', pattern: 'suddenSpike' }],
    } as never);
    vi.mocked(sdk.deleteApiIdentityAdminDemo).mockResolvedValue({ data: undefined } as never);
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    renderWithProviders(<AdminUsersPage />);

    await userEvent.click(screen.getByRole('button', { name: 'Demóadatok létrehozása' }));
    expect(await screen.findByText('demo.spike@futoedzes.local · hirtelen terhelésugrás')).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: 'Demóadatok törlése' }));
    await waitFor(() => expect(sdk.deleteApiIdentityAdminDemo).toHaveBeenCalled());
    await waitFor(() => expect(screen.queryByText(/demo.spike/)).not.toBeInTheDocument());
    expect(within(document.body).queryByText('Létrehozott futók:')).not.toBeInTheDocument();
  });
});
