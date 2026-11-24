import { MantineProvider } from '@mantine/core';
import { DatesProvider } from '@mantine/dates';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render } from '@testing-library/react';
import type { ReactNode } from 'react';
import { MemoryRouter } from 'react-router';
import { theme } from '../app/theme';

/**
 * Komponens renderelése a tesztekhez szükséges keretben: Mantine (átmenetek és portálok nélkül), dátumok, TanStack Query (újrapróbálás
 * nélkül) és router a megadott útvonalon.
 */
export function renderWithProviders(ui: ReactNode, { route = '/' }: { route?: string } = {}) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });

  return render(
    <MantineProvider theme={theme} env="test">
      <DatesProvider settings={{ locale: 'hu', firstDayOfWeek: 1 }}>
        <QueryClientProvider client={queryClient}>
          <MemoryRouter initialEntries={[route]}>{ui}</MemoryRouter>
        </QueryClientProvider>
      </DatesProvider>
    </MantineProvider>,
  );
}

/**
 * Teszt access token a megadott szerepkörökkel (aláírás nélkül; a felület csak a tartalmát olvassa).
 */
export function testAccessToken(roles: string[]): string {
  const payload = btoa(JSON.stringify({ realm_access: { roles } })).replace(/=+$/, '');
  return `fejlec.${payload}.alairas`;
}
