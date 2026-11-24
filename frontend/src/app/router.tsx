import { createBrowserRouter } from 'react-router';
import { AlertsPage } from '../pages/alerts/AlertsPage';
import { CalendarPage } from '../pages/calendar/CalendarPage';
import { AdminRulesPage } from '../pages/admin/AdminRulesPage';
import { AdminUsersPage } from '../pages/admin/AdminUsersPage';
import { CoachOverviewPage } from '../pages/coach/CoachOverviewPage';
import { CoachRunnerPage } from '../pages/coach/CoachRunnerPage';
import { CoachesPage } from '../pages/coaches/CoachesPage';
import { GoalsPage } from '../pages/goals/GoalsPage';
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
      { path: 'calendar', element: <CalendarPage /> },
      { path: 'goals', element: <GoalsPage /> },
      { path: 'alerts', element: <AlertsPage /> },
      { path: 'coaches', element: <CoachesPage /> },
      { path: 'coach', element: <CoachOverviewPage /> },
      { path: 'coach/runners/:runnerId', element: <CoachRunnerPage /> },
      { path: 'admin/users', element: <AdminUsersPage /> },
      { path: 'admin/rules', element: <AdminRulesPage /> },
      { path: '*', element: <NotReadyPage title="Az oldal nem található" /> },
    ],
  },
]);
