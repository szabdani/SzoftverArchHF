import { Alert, Badge, Group, Loader, Select, Stack, Switch, Table, Text, TextInput, Title } from '@mantine/core';
import { useDebouncedValue } from '@mantine/hooks';
import { notifications } from '@mantine/notifications';
import { IconSearch } from '@tabler/icons-react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { useAuth } from 'react-oidc-context';
import { errorMessage } from '../../api/apiError';
import { getApiIdentityAdminUsers, putApiIdentityAdminUsersByIdEnabled, putApiIdentityAdminUsersByIdRole } from '../../api/identity/sdk.gen';
import type { Role } from '../../auth/roles';
import { DemoDataCard } from './DemoDataCard';

/** A választható szerepkörök magyarul. */
const roleOptions: { value: Role; label: string }[] = [
  { value: 'runner', label: 'Futó' },
  { value: 'coach', label: 'Edző' },
  { value: 'admin', label: 'Adminisztrátor' },
];

/**
 * Felhasználókezelés (UC16): keresés név vagy e-mail szerint, szerepkör módosítása, letiltás és
 * engedélyezés. A saját fiók nem módosítható, így az utolsó admin sem zárhatja ki magát. Alatta a
 * demóadatok kezelése (UC18).
 */
export function AdminUsersPage() {
  const auth = useAuth();
  const queryClient = useQueryClient();
  const [search, setSearch] = useState('');
  const [debouncedSearch] = useDebouncedValue(search.trim(), 300);

  const users = useQuery({
    queryKey: ['admin-users', debouncedSearch],
    queryFn: async () => (await getApiIdentityAdminUsers({ query: { search: debouncedSearch || undefined }, throwOnError: true })).data,
  });

  const onSuccess = async () => {
    await queryClient.invalidateQueries({ queryKey: ['admin-users'] });
    notifications.show({ color: 'teal', message: 'A felhasználó módosítva.' });
  };
  const onError = (error: Error) => notifications.show({ color: 'red', message: errorMessage(error) });
  const setRole = useMutation({
    mutationFn: ({ id, role }: { id: string; role: Role }) => putApiIdentityAdminUsersByIdRole({ path: { id }, body: { role }, throwOnError: true }),
    onSuccess,
    onError,
  });
  const setEnabled = useMutation({
    mutationFn: ({ id, enabled }: { id: string; enabled: boolean }) => putApiIdentityAdminUsersByIdEnabled({ path: { id }, body: { enabled }, throwOnError: true }),
    onSuccess,
    onError,
  });

  return (
    <Stack>
      <Title order={2}>Felhasználók</Title>
      <TextInput
        placeholder="Keresés név vagy e-mail szerint"
        aria-label="Keresés"
        leftSection={<IconSearch size={16} />}
        maw={360}
        value={search}
        onChange={(event) => setSearch(event.currentTarget.value)}
      />
      {users.isLoading && <Loader />}
      {users.error && <Alert color="red">{errorMessage(users.error)}</Alert>}
      {users.isSuccess && users.data.length === 0 && <Text c="dimmed">Nincs találat.</Text>}

      {users.data && users.data.length > 0 && (
        <Table.ScrollContainer minWidth={640}>
          <Table>
            <Table.Thead>
              <Table.Tr>
                <Table.Th>Név</Table.Th>
                <Table.Th>E-mail</Table.Th>
                <Table.Th>Szerepkör</Table.Th>
                <Table.Th>Aktív</Table.Th>
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {users.data.map((user) => {
                const isSelf = user.id === auth.user?.profile.sub;
                return (
                  <Table.Tr key={user.id}>
                    <Table.Td>
                      <Group gap="xs" wrap="nowrap">
                        {user.name}
                        {isSelf && <Badge variant="light">Te</Badge>}
                      </Group>
                    </Table.Td>
                    <Table.Td>{user.email ?? '–'}</Table.Td>
                    <Table.Td>
                      <Select
                        aria-label={`${user.name} szerepköre`}
                        data={roleOptions}
                        value={user.role ?? null}
                        placeholder="Nincs"
                        allowDeselect={false}
                        disabled={isSelf}
                        w={170}
                        onChange={(role) => role && setRole.mutate({ id: user.id, role: role as Role })}
                      />
                    </Table.Td>
                    <Table.Td>
                      <Switch
                        aria-label={`${user.name} aktív`}
                        checked={user.enabled}
                        disabled={isSelf}
                        onChange={(event) => setEnabled.mutate({ id: user.id, enabled: event.currentTarget.checked })}
                      />
                    </Table.Td>
                  </Table.Tr>
                );
              })}
            </Table.Tbody>
          </Table>
        </Table.ScrollContainer>
      )}

      <DemoDataCard />
    </Stack>
  );
}
