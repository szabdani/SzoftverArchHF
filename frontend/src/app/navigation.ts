import { IconAdjustments, IconBell, IconCalendar, IconLayoutDashboard, IconRun, IconTarget, IconUserCog, IconUsers, type Icon } from '@tabler/icons-react';
import type { Role } from '../auth/roles';

/** Egy menüpont: felirat, útvonal, ikon. */
export type NavItem = { label: string; to: string; icon: Icon };

/**
 * A menüpontok a felhasználó szerepkörei szerint (spec 3.1): a futó a saját nézeteit, az edző a futóit,
 * az admin a felhasználókezelést látja.
 */
export function navItemsFor(roles: Role[]): NavItem[] {
  const items: NavItem[] = [];

  if (roles.includes('runner')) {
    items.push(
      { label: 'Áttekintés', to: '/', icon: IconLayoutDashboard },
      { label: 'Edzésnapló', to: '/workouts', icon: IconRun },
      { label: 'Naptár', to: '/calendar', icon: IconCalendar },
      { label: 'Célok', to: '/goals', icon: IconTarget },
      { label: 'Figyelmeztetések', to: '/alerts', icon: IconBell },
      { label: 'Edzőim', to: '/coaches', icon: IconUsers },
    );
  }

  if (roles.includes('coach')) {
    items.push({ label: 'Futóim', to: '/coach', icon: IconUsers });
  }

  if (roles.includes('admin')) {
    items.push(
      { label: 'Felhasználók', to: '/admin/users', icon: IconUserCog },
      { label: 'Szabályok', to: '/admin/rules', icon: IconAdjustments },
    );
  }

  return items;
}
