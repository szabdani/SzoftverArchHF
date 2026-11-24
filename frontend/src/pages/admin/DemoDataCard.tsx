import { Button, Card, Group, Stack, Text, Title } from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { errorMessage } from '../../api/apiError';
import { deleteApiIdentityAdminDemo, postApiIdentityAdminDemo } from '../../api/identity/sdk.gen';
import type { DemoRunnerPattern } from '../../api/identity/types.gen';

/** A demó futók mintázatai magyarul. */
const patternLabels: Record<DemoRunnerPattern, string> = {
  steadyBuild: 'egyenletes építkezés',
  suddenSpike: 'hirtelen terhelésugrás',
  frequentSkipping: 'gyakori kihagyás',
  tooManyIntenseDays: 'túl sok intenzív nap',
};

/**
 * Demóadatok létrehozása és törlése (UC18): szimulált futók 12 hétnyi edzéssel, a kockázati szabályok
 * bemutatásához. Az újbóli létrehozás a korábbi demóadatokat lecseréli.
 */
export function DemoDataCard() {
  const queryClient = useQueryClient();
  const create = useMutation({
    mutationFn: async () => (await postApiIdentityAdminDemo({ throwOnError: true })).data,
    onSuccess: () => queryClient.invalidateQueries(),
    onError: (error) => notifications.show({ color: 'red', message: errorMessage(error) }),
  });
  const remove = useMutation({
    mutationFn: () => deleteApiIdentityAdminDemo({ throwOnError: true }),
    onSuccess: async () => {
      create.reset();
      await queryClient.invalidateQueries();
      notifications.show({ color: 'teal', message: 'A demóadatok törölve.' });
    },
    onError: (error) => notifications.show({ color: 'red', message: errorMessage(error) }),
  });

  return (
    <Card withBorder>
      <Title order={4}>Demóadatok</Title>
      <Text size="sm" c="dimmed">
        Szimulált futók 12 hétnyi edzéssel, a kockázati szabályok bemutatásához. Az újbóli létrehozás a korábbi demóadatokat lecseréli.
      </Text>
      <Group mt="sm">
        <Button onClick={() => create.mutate()} loading={create.isPending}>
          Demóadatok létrehozása
        </Button>
        <Button color="red" variant="light" loading={remove.isPending} onClick={() => window.confirm('Biztosan törlöd az összes demóadatot?') && remove.mutate()}>
          Demóadatok törlése
        </Button>
      </Group>
      {create.data && (
        <Stack gap={4} mt="sm">
          <Text size="sm" fw={500}>
            Létrehozott futók:
          </Text>
          {create.data.map((runner) => (
            <Text key={runner.runnerId} size="sm">
              {runner.email} · {patternLabels[runner.pattern]}
            </Text>
          ))}
        </Stack>
      )}
    </Card>
  );
}
