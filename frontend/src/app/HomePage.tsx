import { Navigate } from 'react-router';
import { useRoles } from '../auth/roles';
import { DashboardPage } from '../pages/dashboard/DashboardPage';
import { NotReadyPage } from '../pages/NotReadyPage';

/**
 * A kezdőoldal szerepkör szerint: a futónak a dashboard, az edzőnek a futói, az adminnak a
 * felhasználókezelés.
 */
export function HomePage() {
  const roles = useRoles();

  if (roles.includes('runner')) {
    return <DashboardPage />;
  }

  if (roles.includes('coach')) {
    return <Navigate to="/coach" replace />;
  }

  if (roles.includes('admin')) {
    return <Navigate to="/admin/users" replace />;
  }

  return <NotReadyPage title="Ehhez a fiókhoz nincs szerepkör rendelve" />;
}
