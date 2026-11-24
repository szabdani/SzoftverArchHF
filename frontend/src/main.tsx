import '@mantine/core/styles.css';
import '@mantine/dates/styles.css';
import '@mantine/notifications/styles.css';
import '@mantine/charts/styles.css';
import 'dayjs/locale/hu';

import { MantineProvider } from '@mantine/core';
import { DatesProvider } from '@mantine/dates';
import { Notifications } from '@mantine/notifications';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import dayjs from 'dayjs';
import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { AuthProvider } from 'react-oidc-context';
import { RouterProvider } from 'react-router';
import { ApiError } from './api/apiError';
import { configureApiClients } from './api/setup';
import { router } from './app/router';
import { theme } from './app/theme';
import { getAccessToken, oidcConfig } from './auth/oidcConfig';

dayjs.locale('hu');
configureApiClients(getAccessToken);

// Kliens oldali hibánál (4xx) nincs értelme újrapróbálni, csak hálózati vagy szerverhibánál.
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: (failureCount, error) => !(error instanceof ApiError && error.status < 500) && failureCount < 2,
    },
  },
});

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <MantineProvider theme={theme}>
      <DatesProvider settings={{ locale: 'hu', firstDayOfWeek: 1 }}>
        <Notifications />
        <AuthProvider {...oidcConfig}>
          <QueryClientProvider client={queryClient}>
            <RouterProvider router={router} />
          </QueryClientProvider>
        </AuthProvider>
      </DatesProvider>
    </MantineProvider>
  </StrictMode>,
);
