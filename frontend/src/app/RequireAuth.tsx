import { Button, Center, Loader, Stack, Text } from '@mantine/core';
import { useEffect, type ReactNode } from 'react';
import { useAuth } from 'react-oidc-context';

/**
 * Csak bejelentkezett felhasználónak mutatja a tartalmat; egyébként a Keycloak bejelentkező oldalára
 * irányít (ott regisztrálni is lehet).
 */
export function RequireAuth({ children }: { children: ReactNode }) {
  const auth = useAuth();
  const shouldSignIn = !auth.isLoading && !auth.isAuthenticated && !auth.activeNavigator && !auth.error;

  useEffect(() => {
    if (shouldSignIn) {
      void auth.signinRedirect();
    }
  }, [shouldSignIn, auth]);

  if (auth.error) {
    return (
      <Center h="100vh">
        <Stack align="center">
          <Text>Nem sikerült bejelentkezni: {auth.error.message}</Text>
          <Button onClick={() => void auth.signinRedirect()}>Újrapróbálom</Button>
        </Stack>
      </Center>
    );
  }

  if (!auth.isAuthenticated) {
    return (
      <Center h="100vh">
        <Loader />
      </Center>
    );
  }

  return children;
}
