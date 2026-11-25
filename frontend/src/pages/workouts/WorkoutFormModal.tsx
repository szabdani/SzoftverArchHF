import { Button, Group, Modal, NumberInput, Select, SimpleGrid, Stack, Text, Textarea } from '@mantine/core';
import { DateInput } from '@mantine/dates';
import { useForm } from '@mantine/form';
import { notifications } from '@mantine/notifications';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import dayjs from 'dayjs';
import { errorMessage } from '../../api/apiError';
import { getApiTrainingPlannedWorkouts, postApiTrainingWorkouts, putApiTrainingWorkoutsById } from '../../api/training/sdk.gen';
import type { WorkoutRequest, WorkoutResponse, WorkoutType } from '../../api/training/types.gen';
import { calculateLoad, calculatePace, formatKm, formatPace, parseDuration, workoutTypeLabels } from '../../lib/format';
import { DurationInput } from '../../lib/DurationInput';
import { applyServerErrors } from '../../lib/serverErrors';
import { emptyWorkoutForm, toWorkoutRequest, validateWorkout, workoutToFormValues } from './workoutForm';

const typeOptions = Object.entries(workoutTypeLabels).map(([value, label]) => ({ value, label }));

/**
 * Edzés rögzítése vagy szerkesztése (UC02, UC03). Új edzésnél aznapi tervezett edzéshez is köthető
 * (UC08). A validáció a szerverrel közös sémával fut; a szerver hibái mezőnként jelennek meg.
 */
export function WorkoutFormModal({ opened, onClose, workout }: { opened: boolean; onClose: () => void; workout?: WorkoutResponse }) {
  const queryClient = useQueryClient();
  const today = dayjs().format('YYYY-MM-DD');
  const form = useForm({
    mode: 'controlled',
    initialValues: workout ? workoutToFormValues(workout) : emptyWorkoutForm(today),
    validate: (values) => validateWorkout(values, today),
  });

  // Csak új edzésnél köthető tervezett edzéshez (a szerver módosításkor a kapcsolatot nem változtatja).
  const date = form.values.date;
  const plannedQuery = useQuery({
    queryKey: ['planned-workouts', date, date],
    queryFn: async () => (await getApiTrainingPlannedWorkouts({ query: { from: date!, to: date! }, throwOnError: true })).data,
    enabled: opened && !workout && date !== null,
  });
  const plannedOptions = (plannedQuery.data ?? [])
    .filter((planned) => planned.status !== 'completed')
    .map((planned) => ({
      value: planned.id,
      label: `${workoutTypeLabels[planned.type]}${planned.plannedDistanceKm ? ` · ${formatKm(planned.plannedDistanceKm)}` : ''}${planned.plannedDuration ? ` · ${planned.plannedDuration}` : ''} (${planned.status === 'missed' ? 'kihagyott' : 'tervezett'})`,
    }));

  const mutation = useMutation({
    mutationFn: (body: WorkoutRequest) =>
      workout
        ? putApiTrainingWorkoutsById({ path: { id: workout.id }, body, throwOnError: true })
        : postApiTrainingWorkouts({ body, throwOnError: true }),
    onSuccess: async () => {
      // Az edzés a terhelést, a pontszámot és a figyelmeztetéseket is érinti: minden adat frissül.
      await queryClient.invalidateQueries();
      notifications.show({ color: 'teal', message: workout ? 'Az edzés módosítva.' : 'Az edzés rögzítve.' });
      onClose();
    },
    onError: (error) => {
      if (!applyServerErrors(form, error)) {
        notifications.show({ color: 'red', message: errorMessage(error) });
      }
    },
  });

  // Élő előnézet: tempó és edzésterhelés, ha a táv, az idő és az RPE már értelmezhető.
  const seconds = parseDuration(form.values.duration);
  const distance = Number(form.values.distanceKm);
  const rpe = Number(form.values.rpe);
  const pace = seconds !== null && distance > 0 ? calculatePace(seconds, distance) : null;
  const load = seconds !== null && rpe >= 1 && rpe <= 10 ? calculateLoad(rpe, seconds) : null;

  return (
    <Modal opened={opened} onClose={onClose} title={workout ? 'Edzés módosítása' : 'Új edzés'} size="lg">
      <form onSubmit={form.onSubmit((values) => mutation.mutate(toWorkoutRequest(values)))}>
        <Stack>
          <SimpleGrid cols={{ base: 1, sm: 2 }}>
            <DateInput label="Dátum" withAsterisk maxDate={today} valueFormat="YYYY. MM. DD." key={form.key('date')} {...form.getInputProps('date')} />
            <Select
              label="Típus"
              withAsterisk
              data={typeOptions}
              key={form.key('type')}
              {...form.getInputProps('type')}
              onChange={(value) => form.setFieldValue('type', value as WorkoutType | null)}
            />
            <NumberInput label="Táv (km)" withAsterisk min={0} decimalScale={2} decimalSeparator="," key={form.key('distanceKm')} {...form.getInputProps('distanceKm')} />
            <DurationInput label="Időtartam" withAsterisk value={form.values.duration} onChange={(next) => form.setFieldValue('duration', next)} error={form.errors.duration} />
            <NumberInput label="Átlagpulzus (ütés/perc)" min={0} allowDecimal={false} key={form.key('avgHeartRate')} {...form.getInputProps('avgHeartRate')} />
            <NumberInput label="Emelkedés (m)" min={0} allowDecimal={false} key={form.key('elevationGainM')} {...form.getInputProps('elevationGainM')} />
            <NumberInput label="RPE (érzett terhelés, 1–10)" withAsterisk min={1} allowDecimal={false} key={form.key('rpe')} {...form.getInputProps('rpe')} />
            {plannedOptions.length > 0 && (
              <Select label="Tervezett edzés (opcionális)" data={plannedOptions} clearable key={form.key('plannedWorkoutId')} {...form.getInputProps('plannedWorkoutId')} />
            )}
          </SimpleGrid>
          <Textarea label="Megjegyzés" autosize minRows={2} key={form.key('note')} {...form.getInputProps('note')} />
          <Text size="sm" c="dimmed">
            Tempó: {formatPace(pace)} · Terhelés (sRPE): {load ?? '–'}
          </Text>
          <Group justify="flex-end">
            <Button variant="default" onClick={onClose}>
              Mégse
            </Button>
            <Button type="submit" loading={mutation.isPending}>
              Mentés
            </Button>
          </Group>
        </Stack>
      </form>
    </Modal>
  );
}
