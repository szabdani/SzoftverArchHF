import { useEffect, useRef } from 'react';
import { Button } from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { useAuth } from 'react-oidc-context';
import type { Role } from '../auth/roles';

const NOTIFICATION_ID = 'role-changed';

/**
 * Jelzi, ha a bejelentkezett felhasználó szerepköre megváltozott (pl. az admin futóról edzővé tette).
 * A szerepkört a token hordozza; a csendes token-frissítéskor az új szerepkör már látszik, de a menük és
 * a teljes hozzáférés csak újrabejelentkezés után frissülnek. Ezért ha a szerepkörök eltérnek a munkamenet
 * kezdetén látottaktól, egy értesítés kéri a felhasználót, hogy jelentkezzen be újra.
 * @param roles a felhasználó aktuális szerepkörei (a token alapján).
 */
export function useRoleChangeNotice(roles: Role[]): void {
  const auth = useAuth();
  const initialRoles = useRef<string | null>(null);
  const notified = useRef(false);

  useEffect(() => {
    // A bejelentkezett felhasználó nélkül nincs mit figyelni; kijelentkezéskor a referencia nullázódik.
    if (!auth.isAuthenticated) {
      initialRoles.current = null;
      notified.current = false;
      return;
    }

    const current = [...roles].sort().join(',');

    // Az első (belépéskori) szerepkör-halmaz a viszonyítási alap.
    if (initialRoles.current === null) {
      initialRoles.current = current;
      return;
    }

    if (current !== initialRoles.current && !notified.current) {
      notified.current = true;
      notifications.show({
        id: NOTIFICATION_ID,
        color: 'blue',
        autoClose: false,
        title: 'A szerepköröd megváltozott',
        message: (
          <span>
            Jelentkezz be újra, hogy a módosított hozzáférésed érvénybe lépjen.{' '}
            <Button size="compact-xs" variant="light" ml="xs" onClick={() => void auth.signinRedirect()}>
              Újra bejelentkezés
            </Button>
          </span>
        ),
      });
    }
  }, [auth, roles]);
}
