import { useRoles } from '../auth/roles';
import { DashboardPage } from '../pages/dashboard/DashboardPage';
import { NotReadyPage } from '../pages/NotReadyPage';

/**
 * A kezdőoldal: a futónak a dashboard, a többi szerepkörnek (amíg a nézetük készül) helykitöltő.
 */
export function HomePage() {
  const roles = useRoles();
  return roles.includes('runner') ? <DashboardPage /> : <NotReadyPage title="Áttekintés" />;
}
