import { Button, Group, Modal, NumberInput, Select, SimpleGrid, Stack, TextInput, Textarea } from '@mantine/core';
import { DateInput } from '@mantine/dates';
import { useForm } from '@mantine/form';
import { notifications } from '@mantine/notifications';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { errorMessage } from '../../api/apiError';
import {
  deleteApiTrainingPlansByPlanIdWorkoutsById,
  postApiTrainingPlansByPlanIdWorkouts,
  putApiTrainingPlansByPlanIdWorkoutsById,
} from '../../api/training/sdk.gen';
import type { PlannedWorkoutRequest, PlannedWorkoutResponse, PlanResponse, WorkoutType } from '../../api/training/types.gen';
import { formatDate, workoutTypeLabels } from '../../lib/format';
import { applyServerErrors } from '../../lib/serverErrors';
import { plannedToFormValues, toPlannedWorkoutRequest, validatePlannedWorkout } from './planForms';

const typeOptions = Object.entries(workoutTypeLabels).map(([value, label]) => ({ value, label }));

/**
 * Tervezett edzés felvétele egy tervbe, vagy egy meglévő módosítása és törlése (UC11).
 * Új edzésnél a naptárban kiválasztott nap és (ha csak egy van) a terv előre ki van töltve.
 */
export function PlannedWorkoutFormModal({
  onClose,
  plans,
  item,
  date,
}: {
  onClose: () => void;
  plans: PlanResponse[];
  item?: PlannedWorkoutResponse;
  date?: string;
}) {
  const queryClient = useQueryClient();
  const defaultPlanId = item?.planId ?? (plans.length === 1 ? plans[0].id : null);
  const form = useForm({
    mode: 'controlled',
    initialValues: plannedToFormValues(item, defaultPlanId, date ?? null),
    validate: (values) => validatePlannedWorkout(values, plans.find((plan) => plan.id === values.planId)),
  });
  const plan = plans.find((candidate) => candidate.id === form.values.planId);

  /** Mentés vagy törlés után minden adat frissül, és az ablak bezárul. */
  const onSuccess = async (message: string) => {
    await queryClient.invalidateQueries();
    notifications.show({ color: 'teal', message });
    onClose();
  };

  const save = useMutation({
    mutationFn: ({ planId, body }: { planId: string; body: PlannedWorkoutRequest }) =>
      item
        ? putApiTrainingPlansByPlanIdWorkoutsById({ path: { planId, id: item.id }, body, throwOnError: true })
        : postApiTrainingPlansByPlanIdWorkouts({ path: { planId }, body, throwOnError: true }),
    onSuccess: () => onSuccess(item ? 'A tervezett edzés módosítva.' : 'A tervezett edzés felvéve.'),
    onError: (error) => {
      if (!applyServerErrors(form, error)) {
        notifications.show({ color: 'red', message: errorMessage(error) });
      }
    },
  });
  const remove = useMutation({
    mutationFn: (target: PlannedWorkoutResponse) =>
      deleteApiTrainingPlansByPlanIdWorkoutsById({ path: { planId: target.planId, id: target.id }, throwOnError: true }),
    onSuccess: () => onSuccess('A tervezett edzés törölve.'),
    onError: (error) => notifications.show({ color: 'red', message: errorMessage(error) }),
  });

  return (
    <Modal opened onClose={onClose} title={item ? 'Tervezett edzés' : 'Új tervezett edzés'} size="lg">
      <form onSubmit={form.onSubmit((values) => save.mutate({ planId: values.planId!, body: toPlannedWorkoutRequest(values) }))}>
        <Stack>
          <SimpleGrid cols={{ base: 1, sm: 2 }}>
            <Select
              label="Terv"
              withAsterisk
              disabled={item !== undefined}
              data={plans.map((candidate) => ({ value: candidate.id, label: `${candidate.name} (${formatDate(candidate.startDate)} – ${formatDate(candidate.endDate)})` }))}
              key={form.key('planId')}
              {...form.getInputProps('planId')}
            />
            <DateInput
              label="Dátum"
              withAsterisk
              minDate={plan?.startDate}
              maxDate={plan?.endDate}
              valueFormat="YYYY. MM. DD."
              key={form.key('date')}
              {...form.getInputProps('date')}
            />
            <Select
              label="Típus"
              withAsterisk
              data={typeOptions}
              key={form.key('type')}
              {...form.getInputProps('type')}
              onChange={(value) => form.setFieldValue('type', value as WorkoutType | null)}
            />
            <NumberInput label="Cél RPE (1–10)" withAsterisk min={1} max={10} allowDecimal={false} key={form.key('targetRpe')} {...form.getInputProps('targetRpe')} />
            <NumberInput label="Tervezett táv (km)" min={0} decimalScale={2} decimalSeparator="," key={form.key('plannedDistanceKm')} {...form.getInputProps('plannedDistanceKm')} />
            <TextInput label="Tervezett időtartam" placeholder="óó:pp:mm" key={form.key('plannedDuration')} {...form.getInputProps('plannedDuration')} />
          </SimpleGrid>
          <Textarea label="Megjegyzés" autosize minRows={2} key={form.key('note')} {...form.getInputProps('note')} />
          <Group justify="space-between">
            {item ? (
              <Button color="red" variant="subtle" loading={remove.isPending} onClick={() => remove.mutate(item)}>
                Törlés
              </Button>
            ) : (
              <span />
            )}
            <Group>
              <Button variant="default" onClick={onClose}>
                Mégse
              </Button>
              <Button type="submit" loading={save.isPending}>
                Mentés
              </Button>
            </Group>
          </Group>
        </Stack>
      </form>
    </Modal>
  );
}
