import { createBrowserRouter } from 'react-router';
import { AlertsPage } from '../pages/alerts/AlertsPage';
import { NotReadyPage } from '../pages/NotReadyPage';
import { WorkoutsPage } from '../pages/workouts/WorkoutsPage';
import { AppLayout } from './AppLayout';
import { HomePage } from './HomePage';
import { RequireAuth } from './RequireAuth';

/**
 * Az alkalmazás útvonalai; mindegyik bejelentkezéshez kötött, és az `AppLayout` keretben jelenik meg.
 */
export const router = createBrowserRouter([
  {
    path: '/',
    element: (
      <RequireAuth>
        <AppLayout />
      </RequireAuth>
    ),
    children: [
      { index: true, element: <HomePage /> },
      { path: 'workouts', element: <WorkoutsPage /> },
      { path: 'alerts', element: <AlertsPage /> },
      { path: 'coach', element: <NotReadyPage title="Futóim" /> },
      { path: 'admin/users', element: <NotReadyPage title="Felhasználók" /> },
      { path: 'admin/rules', element: <NotReadyPage title="Szabályok" /> },
      { path: '*', element: <NotReadyPage title="Az oldal nem található" /> },
    ],
  },
]);
