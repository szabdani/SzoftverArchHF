import { useEffect, useRef } from 'react';
import { notifications } from '@mantine/notifications';
import { useQuery } from '@tanstack/react-query';
import { getApiIdentityInvitationsReceived } from '../api/identity/sdk.gen';
import type { Role } from '../auth/roles';

/**
 * A futónak szóló függő edzői meghívók száma, és egy egyszeri értesítés belépés után, ha van ilyen
 * (UC12/UC13). Csak futó szerepkörnél kérdez le; a szám a menü badge-ét táplálja, az értesítés pedig
 * felhívja a figyelmet a meghívóra, amit különben csak az „Edzőim” oldalon látna a futó.
 * @returns a függő meghívók száma (0, ha nincs vagy nem futó a felhasználó).
 */
export function usePendingInvitations(roles: Role[]): number {
  const isRunner = roles.includes('runner');
  const received = useQuery({
    queryKey: ['invitations', 'received'],
    queryFn: async () => (await getApiIdentityInvitationsReceived({ throwOnError: true })).data,
    enabled: isRunner,
  });

  const count = received.data?.length ?? 0;
  const notified = useRef(false);

  // Belépés után egyszer jelezzük, ha van függő meghívó; a badge végig mutatja a számot.
  useEffect(() => {
    if (!notified.current && count > 0) {
      notified.current = true;
      notifications.show({
        color: 'blue',
        title: 'Új edzői meghívó',
        message: count === 1 ? 'Egy függő edzői meghívód van. Nézd meg az „Edzőim” oldalon.' : `${count} függő edzői meghívód van. Nézd meg az „Edzőim” oldalon.`,
      });
    }
  }, [count]);

  return isRunner ? count : 0;
}
