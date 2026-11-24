import { screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { renderWithProviders, testAccessToken } from '../test/render';
import { AppLayout } from './AppLayout';

const auth = vi.hoisted(() => ({ current: { user: { access_token: '', profile: { email: 'runner@futoedzes.local' } }, signoutRedirect: () => {} } }));
vi.mock('react-oidc-context', () => ({ useAuth: () => auth.current }));

/** Az alkalmazás kerete a bejelentkezett felhasználóval. */
describe('AppLayout', () => {
  it('a futónak a saját menüpontjait és az e-mail címét mutatja', () => {
    auth.current.user.access_token = testAccessToken(['runner', 'offline_access']);

    renderWithProviders(<AppLayout />);

    expect(screen.getByText('runner@futoedzes.local')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Edzésnapló' })).toHaveAttribute('href', '/workouts');
    expect(screen.queryByRole('link', { name: 'Felhasználók' })).not.toBeInTheDocument();
  });

  it('az adminnak csak a felhasználókezelést', () => {
    auth.current.user.access_token = testAccessToken(['admin']);

    renderWithProviders(<AppLayout />);

    expect(screen.getByRole('link', { name: 'Felhasználók' })).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Edzésnapló' })).not.toBeInTheDocument();
  });
});
