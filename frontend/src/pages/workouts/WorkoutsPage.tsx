import { ActionIcon, Alert, Badge, Button, Group, Loader, Select, Stack, Table, Text, Title, UnstyledButton } from '@mantine/core';
import { DatePickerInput } from '@mantine/dates';
import { IconChevronDown, IconChevronUp, IconPencil, IconPlus, IconTrash } from '@tabler/icons-react';
import { useQuery } from '@tanstack/react-query';
import dayjs from 'dayjs';
import { useState, type ReactNode } from 'react';
import { errorMessage } from '../../api/apiError';
import { getApiTrainingWorkouts } from '../../api/training/sdk.gen';
import type { WorkoutResponse, WorkoutType } from '../../api/training/types.gen';
import { formatKm, formatPace, workoutTypeLabels } from '../../lib/format';
import { DeleteWorkoutModal } from './DeleteWorkoutModal';
import { WorkoutFormModal } from './WorkoutFormModal';
import { filterAndSortWorkouts, type WorkoutSort } from './workoutList';

const typeOptions = Object.entries(workoutTypeLabels).map(([value, label]) => ({ value, label }));

/**
 * Edzésnapló (UC04): a futó edzései egy időszakban (alapból az utolsó 12 hét), típus szerint szűrve és
 * rendezve; innen lehet rögzíteni, módosítani és törölni (UC02, UC03).
 */
export function WorkoutsPage() {
  const today = dayjs().format('YYYY-MM-DD');
  const [range, setRange] = useState<[string | null, string | null]>([dayjs().subtract(83, 'day').format('YYYY-MM-DD'), today]);
  const [type, setType] = useState<WorkoutType | null>(null);
  const [sort, setSort] = useState<WorkoutSort>({ field: 'date', descending: true });
  const [editing, setEditing] = useState<WorkoutResponse | 'new' | null>(null);
  const [deleting, setDeleting] = useState<WorkoutResponse | null>(null);

  const [from, to] = range;
  const query = useQuery({
    queryKey: ['workouts', from, to],
    queryFn: async () => (await getApiTrainingWorkouts({ query: { from: from!, to: to! }, throwOnError: true })).data,
    enabled: from !== null && to !== null,
  });
  const workouts = filterAndSortWorkouts(query.data ?? [], type, sort);

  /** Rendezhető oszlopfejléc: kattintásra az oszlop szerint rendez, újra kattintva megfordítja. */
  const sortHeader = (field: WorkoutSort['field'], label: string): ReactNode => (
    <UnstyledButton onClick={() => setSort({ field, descending: sort.field === field ? !sort.descending : true })}>
      <Group gap={4} wrap="nowrap">
        <Text fw={700} size="sm">
          {label}
        </Text>
        {sort.field === field && (sort.descending ? <IconChevronDown size={14} /> : <IconChevronUp size={14} />)}
      </Group>
    </UnstyledButton>
  );

  return (
    <Stack>
      <Group justify="space-between">
        <Title order={2}>Edzésnapló</Title>
        <Button leftSection={<IconPlus size={16} />} onClick={() => setEditing('new')}>
          Új edzés
        </Button>
      </Group>

      <Group align="flex-end">
        <DatePickerInput type="range" label="Időszak" value={range} onChange={setRange} maxDate={today} valueFormat="YYYY. MM. DD." w={280} />
        <Select label="Típus" placeholder="Mind" data={typeOptions} value={type} onChange={(value) => setType(value as WorkoutType | null)} clearable w={180} />
      </Group>

      {query.isError && <Alert color="red">{errorMessage(query.error)}</Alert>}
      {query.isLoading && <Loader />}
      {query.isSuccess && workouts.length === 0 && <Text c="dimmed">Ebben az időszakban nincs edzés.</Text>}

      {workouts.length > 0 && (
        <Table.ScrollContainer minWidth={760}>
          <Table striped highlightOnHover>
            <Table.Thead>
              <Table.Tr>
                <Table.Th>{sortHeader('date', 'Dátum')}</Table.Th>
                <Table.Th>Típus</Table.Th>
                <Table.Th>{sortHeader('distanceKm', 'Táv')}</Table.Th>
                <Table.Th>Idő</Table.Th>
                <Table.Th>Tempó</Table.Th>
                <Table.Th>Pulzus</Table.Th>
                <Table.Th>Emelkedés</Table.Th>
                <Table.Th>RPE</Table.Th>
                <Table.Th>{sortHeader('load', 'Terhelés')}</Table.Th>
                <Table.Th />
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {workouts.map((workout) => (
                <Table.Tr key={workout.id}>
                  <Table.Td>{dayjs(workout.date).format('YYYY. MM. DD.')}</Table.Td>
                  <Table.Td>
                    <Group gap={4} wrap="nowrap">
                      <Badge variant="light">{workoutTypeLabels[workout.type]}</Badge>
                      {workout.plannedWorkoutId && <Badge variant="outline" color="gray" title="Tervezett edzést teljesített">terv</Badge>}
                    </Group>
                  </Table.Td>
                  <Table.Td>{formatKm(workout.distanceKm)}</Table.Td>
                  <Table.Td>{workout.duration}</Table.Td>
                  <Table.Td>{formatPace(workout.paceSecondsPerKm)}</Table.Td>
                  <Table.Td>{workout.avgHeartRate ?? '–'}</Table.Td>
                  <Table.Td>{workout.elevationGainM != null ? `${workout.elevationGainM} m` : '–'}</Table.Td>
                  <Table.Td>{workout.rpe}</Table.Td>
                  <Table.Td>{workout.load}</Table.Td>
                  <Table.Td>
                    <Group gap={4} wrap="nowrap">
                      <ActionIcon variant="subtle" aria-label="Módosítás" onClick={() => setEditing(workout)}>
                        <IconPencil size={16} />
                      </ActionIcon>
                      <ActionIcon variant="subtle" color="red" aria-label="Törlés" onClick={() => setDeleting(workout)}>
                        <IconTrash size={16} />
                      </ActionIcon>
                    </Group>
                  </Table.Td>
                </Table.Tr>
              ))}
            </Table.Tbody>
          </Table>
        </Table.ScrollContainer>
      )}

      {editing !== null && (
        <WorkoutFormModal
          key={editing === 'new' ? 'new' : editing.id}
          opened
          onClose={() => setEditing(null)}
          workout={editing === 'new' ? undefined : editing}
        />
      )}
      <DeleteWorkoutModal workout={deleting} onClose={() => setDeleting(null)} />
    </Stack>
  );
}
