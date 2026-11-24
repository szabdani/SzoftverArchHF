import { createBrowserRouter } from 'react-router';
import { NotReadyPage } from '../pages/NotReadyPage';
import { WorkoutsPage } from '../pages/workouts/WorkoutsPage';
import { AppLayout } from './AppLayout';
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
      { index: true, element: <NotReadyPage title="Áttekintés" /> },
      { path: 'workouts', element: <WorkoutsPage /> },
      { path: 'alerts', element: <NotReadyPage title="Figyelmeztetések" /> },
      { path: 'coach', element: <NotReadyPage title="Futóim" /> },
      { path: 'admin/users', element: <NotReadyPage title="Felhasználók" /> },
      { path: 'admin/rules', element: <NotReadyPage title="Szabályok" /> },
      { path: '*', element: <NotReadyPage title="Az oldal nem található" /> },
    ],
  },
]);
