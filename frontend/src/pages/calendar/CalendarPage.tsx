import '@fullcalendar/react/skeleton.css';
import '@fullcalendar/react/themes/classic/theme.css';
import '@fullcalendar/react/themes/classic/palette.css';

import FullCalendar, { type DateClickInfo, type DatesSetInfo, type EventClickInfo, type EventDropInfo } from '@fullcalendar/react';
import dayGridPlugin from '@fullcalendar/react/daygrid';
import interactionPlugin from '@fullcalendar/react/interaction';
import huLocale from '@fullcalendar/react/locales/hu';
import classicThemePlugin from '@fullcalendar/react/themes/classic';
import { ActionIcon, Alert, Badge, Button, Card, Group, Stack, Text, Title } from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { IconCalendarPlus, IconPencil, IconPlus, IconTrash } from '@tabler/icons-react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import dayjs from 'dayjs';
import { useState } from 'react';
import { errorMessage } from '../../api/apiError';
import {
  deleteApiTrainingPlansById,
  getApiTrainingPlannedWorkouts,
  getApiTrainingPlans,
  getApiTrainingWorkouts,
  putApiTrainingPlansByPlanIdWorkoutsById,
} from '../../api/training/sdk.gen';
import type { PlannedWorkoutResponse, PlanResponse } from '../../api/training/types.gen';
import { formatDate } from '../../lib/format';
import { moveRequest, toCalendarEvents } from './calendarEvents';
import { PlanFormModal } from './PlanFormModal';
import { PlannedWorkoutFormModal } from './PlannedWorkoutFormModal';

/** A nyitott ablak: terv (új vagy módosítás) vagy tervezett edzés (új egy napra, vagy meglévő). */
type Dialog =
  | { kind: 'plan'; plan?: PlanResponse }
  | { kind: 'planned'; item?: PlannedWorkoutResponse; date?: string }
  | null;

/**
 * Edzésnaptár és tervkezelés (UC07, UC11): havi vagy heti nézet az elvégzett és a tervezett edzésekkel,
 * a tervezett edzések húzással áthelyezhetők. Alatta a tervek listája. A futó a saját magának, az edző
 * (runnerId megadásával) a hozzárendelt futónak kezeli a terveket.
 */
export function CalendarPage({ runnerId }: { runnerId?: string }) {
  const queryClient = useQueryClient();
  const [range, setRange] = useState<{ from: string; to: string } | null>(null);
  const [dialog, setDialog] = useState<Dialog>(null);
  const [deleting, setDeleting] = useState<string | null>(null);

  const plans = useQuery({
    queryKey: ['plans', runnerId],
    queryFn: async () => (await getApiTrainingPlans({ query: { runnerId }, throwOnError: true })).data,
  });
  const planned = useQuery({
    queryKey: ['planned-workouts', range?.from, range?.to, runnerId],
    queryFn: async () => (await getApiTrainingPlannedWorkouts({ query: { runnerId, from: range!.from, to: range!.to }, throwOnError: true })).data,
    enabled: range !== null,
  });
  const workouts = useQuery({
    queryKey: ['workouts', range?.from, range?.to, runnerId],
    queryFn: async () => (await getApiTrainingWorkouts({ query: { runnerId, from: range!.from, to: range!.to }, throwOnError: true })).data,
    enabled: range !== null,
  });

  const move = useMutation({
    mutationFn: ({ item, date }: { item: PlannedWorkoutResponse; date: string }) =>
      putApiTrainingPlansByPlanIdWorkoutsById({ path: { planId: item.planId, id: item.id }, body: moveRequest(item, date), throwOnError: true }),
    onSuccess: () => queryClient.invalidateQueries(),
  });
  const deletePlan = useMutation({
    mutationFn: (id: string) => deleteApiTrainingPlansById({ path: { id }, throwOnError: true }),
    onSuccess: async () => {
      await queryClient.invalidateQueries();
      notifications.show({ color: 'teal', message: 'A terv törölve.' });
    },
    onError: (error) => notifications.show({ color: 'red', message: errorMessage(error) }),
    onSettled: () => setDeleting(null),
  });

  /** A látható időszak változásakor (lapozás, nézetváltás) ehhez kérjük az adatokat; a vég nem inkluzív. */
  const handleDatesSet = (info: DatesSetInfo) =>
    setRange({ from: dayjs(info.start).format('YYYY-MM-DD'), to: dayjs(info.end).subtract(1, 'day').format('YYYY-MM-DD') });

  /** Áthelyezés húzással: siker esetén frissül minden, hiba (pl. a terv időszakán kívül) esetén visszaugrik. */
  const handleEventDrop = (info: EventDropInfo) => {
    const item = info.event.extendedProps.planned as PlannedWorkoutResponse | undefined;
    if (!item || !info.event.start) {
      info.revert();
      return;
    }

    move.mutate(
      { item, date: dayjs(info.event.start).format('YYYY-MM-DD') },
      {
        onError: (error) => {
          info.revert();
          notifications.show({ color: 'red', message: errorMessage(error) });
        },
      },
    );
  };

  /** Tervezett edzésre kattintva megnyílik a módosító ablak; az elvégzett edzés a naplóban szerkeszthető. */
  const handleEventClick = (info: EventClickInfo) => {
    const item = info.event.extendedProps.planned as PlannedWorkoutResponse | undefined;
    if (item) {
      setDialog({ kind: 'planned', item });
    }
  };

  /** Üres napra kattintva új tervezett edzés arra a napra (ha van terv). */
  const handleDateClick = (info: DateClickInfo) => {
    if ((plans.data ?? []).length > 0) {
      setDialog({ kind: 'planned', date: info.dateStr.slice(0, 10) });
    }
  };

  const error = plans.error ?? planned.error ?? workouts.error;

  return (
    <Stack>
      <Group justify="space-between">
        <Title order={2}>Naptár</Title>
        <Group>
          <Button variant="default" leftSection={<IconCalendarPlus size={16} />} onClick={() => setDialog({ kind: 'plan' })}>
            Új terv
          </Button>
          <Button leftSection={<IconPlus size={16} />} disabled={(plans.data ?? []).length === 0} onClick={() => setDialog({ kind: 'planned' })}>
            Új tervezett edzés
          </Button>
        </Group>
      </Group>

      {error && <Alert color="red">{errorMessage(error)}</Alert>}

      <Group gap="xs">
        <Badge color="teal">✓ Elvégzett</Badge>
        <Badge color="blue">Tervezett</Badge>
        <Badge color="red">Kihagyott</Badge>
        <Text size="xs" c="dimmed">
          A tervezett edzések áthúzhatók másik napra.
        </Text>
      </Group>

      <Card withBorder>
        <FullCalendar
          plugins={[dayGridPlugin, interactionPlugin, classicThemePlugin]}
          locale={huLocale}
          firstDay={1}
          initialView="dayGridMonth"
          headerToolbar={{ start: 'prev,next today', center: 'title', end: 'dayGridMonth,dayGridWeek' }}
          height="auto"
          events={toCalendarEvents(planned.data ?? [], workouts.data ?? [])}
          datesSet={handleDatesSet}
          eventDrop={handleEventDrop}
          eventClick={handleEventClick}
          dateClick={handleDateClick}
        />
      </Card>

      <Title order={3}>Edzéstervek</Title>
      {plans.data?.length === 0 && <Text c="dimmed">Még nincs edzésterv. Készíts egyet az „Új terv” gombbal.</Text>}
      {(plans.data ?? []).map((plan) => (
        <Card key={plan.id} withBorder>
          <Group justify="space-between">
            <div>
              <Group gap="xs">
                <Text fw={700}>{plan.name}</Text>
                {plan.goalId && <Badge variant="light">célhoz kötve</Badge>}
              </Group>
              <Text size="sm" c="dimmed">
                {formatDate(plan.startDate)} – {formatDate(plan.endDate)} · {plan.workouts.length} tervezett edzés (
                {plan.workouts.filter((item) => item.status === 'completed').length} teljesített,{' '}
                {plan.workouts.filter((item) => item.status === 'missed').length} kihagyott)
              </Text>
            </div>
            <Group gap={4}>
              <ActionIcon variant="subtle" aria-label="Terv módosítása" onClick={() => setDialog({ kind: 'plan', plan })}>
                <IconPencil size={16} />
              </ActionIcon>
              <ActionIcon
                variant="subtle"
                color="red"
                aria-label="Terv törlése"
                loading={deleting === plan.id}
                onClick={() => {
                  // A terv a tervezett edzéseivel együtt törlődik, ezért megerősítést kérünk.
                  if (window.confirm(`Biztosan törlöd a(z) „${plan.name}” tervet a tervezett edzéseivel együtt?`)) {
                    setDeleting(plan.id);
                    deletePlan.mutate(plan.id);
                  }
                }}
              >
                <IconTrash size={16} />
              </ActionIcon>
            </Group>
          </Group>
        </Card>
      ))}

      {dialog?.kind === 'plan' && <PlanFormModal onClose={() => setDialog(null)} plan={dialog.plan} runnerId={runnerId} />}
      {dialog?.kind === 'planned' && (
        <PlannedWorkoutFormModal onClose={() => setDialog(null)} plans={plans.data ?? []} item={dialog.item} date={dialog.date} />
      )}
    </Stack>
  );
}
