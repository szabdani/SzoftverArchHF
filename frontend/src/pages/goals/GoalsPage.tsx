import { Alert, Badge, Button, Card, Group, Loader, Stack, Table, Text, Title } from '@mantine/core';
import { IconPencil, IconTarget } from '@tabler/icons-react';
import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { ApiError, errorMessage } from '../../api/apiError';
import { getApiTrainingGoals, getApiTrainingGoalsActive } from '../../api/training/sdk.gen';
import { formatDate, formatKm, goalTypeLabels, workoutTypeLabels } from '../../lib/format';
import { GoalFormModal } from './GoalFormModal';

/**
 * Célok (UC05): az aktív cél a céldátumig hátralévő hetekkel és a legutóbbi edzésekkel, valamint a
 * korábbi célok. A futó kitűzheti és módosíthatja; az edző (runnerId megadásával) csak megtekinti.
 */
export function GoalsPage({ runnerId, readOnly = false }: { runnerId?: string; readOnly?: boolean }) {
  const [editing, setEditing] = useState<'new' | 'active' | null>(null);

  // Ha nincs aktív cél, a szerver 404-et ad: ez nem hiba, hanem „nincs cél”.
  const active = useQuery({
    queryKey: ['goals', 'active', runnerId],
    queryFn: async () => {
      try {
        return (await getApiTrainingGoalsActive({ query: { runnerId }, throwOnError: true })).data;
      } catch (error) {
        if (error instanceof ApiError && error.status === 404) {
          return null;
        }

        throw error;
      }
    },
  });
  const all = useQuery({
    queryKey: ['goals', 'all', runnerId],
    queryFn: async () => (await getApiTrainingGoals({ query: { runnerId }, throwOnError: true })).data,
  });

  const activeGoal = active.data?.goal;
  const archived = (all.data ?? []).filter((goal) => !goal.isActive);

  return (
    <Stack>
      <Group justify="space-between">
        <Title order={2}>Célok</Title>
        {!readOnly && (
          <Button leftSection={<IconTarget size={16} />} onClick={() => setEditing('new')}>
            Új cél
          </Button>
        )}
      </Group>

      {(active.error ?? all.error) && <Alert color="red">{errorMessage(active.error ?? all.error)}</Alert>}
      {active.isLoading && <Loader />}

      {active.data === null && <Text c="dimmed">Nincs aktív cél.{!readOnly && ' Tűzz ki egyet az „Új cél” gombbal.'}</Text>}

      {active.data && activeGoal && (
        <Card withBorder>
          <Group justify="space-between" align="flex-start">
            <div>
              <Group gap="sm">
                <Title order={3}>{goalTypeLabels[activeGoal.type]}</Title>
                <Badge variant="light">{formatKm(activeGoal.distanceKm)}</Badge>
              </Group>
              <Text mt={4}>
                Céldátum: {formatDate(activeGoal.targetDate)}
                {activeGoal.targetTime && ` · célidő: ${activeGoal.targetTime}`}
              </Text>
              <Text fw={700} size="lg" mt="xs">
                Hátralévő hetek: {active.data.weeksRemaining}
              </Text>
            </div>
            {!readOnly && (
              <Button variant="default" leftSection={<IconPencil size={16} />} onClick={() => setEditing('active')}>
                Módosítás
              </Button>
            )}
          </Group>

          <Title order={5} mt="md" mb="xs">
            Legutóbbi edzések
          </Title>
          {active.data.recentWorkouts.length === 0 ? (
            <Text c="dimmed" size="sm">
              Még nincs edzés.
            </Text>
          ) : (
            <Table>
              <Table.Tbody>
                {active.data.recentWorkouts.map((workout) => (
                  <Table.Tr key={workout.id}>
                    <Table.Td>{formatDate(workout.date)}</Table.Td>
                    <Table.Td>{workoutTypeLabels[workout.type]}</Table.Td>
                    <Table.Td>{formatKm(workout.distanceKm)}</Table.Td>
                    <Table.Td>{workout.duration}</Table.Td>
                    <Table.Td>RPE {workout.rpe}</Table.Td>
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
          )}
        </Card>
      )}

      {archived.length > 0 && (
        <Card withBorder>
          <Title order={5} mb="xs">
            Korábbi célok
          </Title>
          <Table>
            <Table.Tbody>
              {archived.map((goal) => (
                <Table.Tr key={goal.id}>
                  <Table.Td>{goalTypeLabels[goal.type]}</Table.Td>
                  <Table.Td>{formatKm(goal.distanceKm)}</Table.Td>
                  <Table.Td>Céldátum: {formatDate(goal.targetDate)}</Table.Td>
                  <Table.Td>{goal.archivedAt && `Archiválva: ${formatDate(goal.archivedAt)}`}</Table.Td>
                </Table.Tr>
              ))}
            </Table.Tbody>
          </Table>
        </Card>
      )}

      {editing !== null && (
        <GoalFormModal opened onClose={() => setEditing(null)} goal={editing === 'active' ? activeGoal : undefined} />
      )}
    </Stack>
  );
}
