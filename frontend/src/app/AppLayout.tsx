import { AppShell, Burger, Button, Group, NavLink, Text, Title } from '@mantine/core';
import { useDisclosure } from '@mantine/hooks';
import { IconLogout } from '@tabler/icons-react';
import { useAuth } from 'react-oidc-context';
import { Link, Outlet, useLocation } from 'react-router';
import { useRoles } from '../auth/roles';
import { navItemsFor } from './navigation';

/**
 * Az alkalmazás kerete: fejléc a felhasználóval és kilépéssel, oldalsó menü a szerepkörök szerint
 * (mobilon lenyíló), és a tartalom.
 */
export function AppLayout() {
  const auth = useAuth();
  const roles = useRoles();
  const location = useLocation();
  const [opened, { toggle, close }] = useDisclosure();

  return (
    <AppShell header={{ height: 56 }} navbar={{ width: 240, breakpoint: 'sm', collapsed: { mobile: !opened } }} padding="md">
      <AppShell.Header>
        <Group h="100%" px="md" justify="space-between" wrap="nowrap">
          <Group gap="sm" wrap="nowrap">
            <Burger opened={opened} onClick={toggle} hiddenFrom="sm" size="sm" aria-label="Menü" />
            <Title order={4}>Futóedzés-tervező</Title>
          </Group>
          <Group gap="sm" wrap="nowrap">
            <Text size="sm" c="dimmed" visibleFrom="xs">
              {auth.user?.profile.email}
            </Text>
            <Button variant="subtle" leftSection={<IconLogout size={16} />} onClick={() => void auth.signoutRedirect()}>
              Kilépés
            </Button>
          </Group>
        </Group>
      </AppShell.Header>

      <AppShell.Navbar p="xs">
        {navItemsFor(roles).map((item) => (
          <NavLink
            key={item.to}
            component={Link}
            to={item.to}
            label={item.label}
            leftSection={<item.icon size={18} />}
            active={location.pathname === item.to}
            onClick={close}
          />
        ))}
      </AppShell.Navbar>

      <AppShell.Main>
        <Outlet />
      </AppShell.Main>
    </AppShell>
  );
}
