import { Button, Group, Modal, NumberInput, Select, Stack, TextInput } from '@mantine/core';
import { DateInput } from '@mantine/dates';
import { useForm } from '@mantine/form';
import { notifications } from '@mantine/notifications';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import dayjs from 'dayjs';
import { errorMessage } from '../../api/apiError';
import { postApiTrainingGoals, putApiTrainingGoalsById } from '../../api/training/sdk.gen';
import type { GoalRequest, GoalResponse, GoalType } from '../../api/training/types.gen';
import { goalTypeLabels } from '../../lib/format';
import { applyServerErrors } from '../../lib/serverErrors';
import { emptyGoalForm, goalToFormValues, toGoalRequest, validateGoal } from './goalForm';

const typeOptions = Object.entries(goalTypeLabels).map(([value, label]) => ({ value, label }));

/**
 * Új cél kitűzése (az előző aktív cél archiválódik) vagy az aktív cél módosítása (UC05).
 */
export function GoalFormModal({ opened, onClose, goal }: { opened: boolean; onClose: () => void; goal?: GoalResponse }) {
  const queryClient = useQueryClient();
  const today = dayjs().format('YYYY-MM-DD');
  const form = useForm({
    mode: 'controlled',
    initialValues: goal ? goalToFormValues(goal) : emptyGoalForm(),
    validate: (values) => validateGoal(values, today),
  });

  const mutation = useMutation({
    mutationFn: (body: GoalRequest) =>
      goal ? putApiTrainingGoalsById({ path: { id: goal.id }, body, throwOnError: true }) : postApiTrainingGoals({ body, throwOnError: true }),
    onSuccess: async () => {
      await queryClient.invalidateQueries();
      notifications.show({ color: 'teal', message: goal ? 'A cél módosítva.' : 'Az új cél kitűzve.' });
      onClose();
    },
    onError: (error) => {
      if (!applyServerErrors(form, error)) {
        notifications.show({ color: 'red', message: errorMessage(error) });
      }
    },
  });

  return (
    <Modal opened={opened} onClose={onClose} title={goal ? 'Cél módosítása' : 'Új cél'}>
      <form onSubmit={form.onSubmit((values) => mutation.mutate(toGoalRequest(values)))}>
        <Stack>
          <Select
            label="Céltípus"
            withAsterisk
            data={typeOptions}
            key={form.key('type')}
            {...form.getInputProps('type')}
            onChange={(value) => form.setFieldValue('type', value as GoalType | null)}
          />
          {form.values.type === 'custom' && (
            <NumberInput label="Táv (km)" withAsterisk min={0} decimalScale={2} decimalSeparator="," key={form.key('distanceKm')} {...form.getInputProps('distanceKm')} />
          )}
          <DateInput label="Céldátum" withAsterisk minDate={today} valueFormat="YYYY. MM. DD." key={form.key('targetDate')} {...form.getInputProps('targetDate')} />
          <TextInput label="Célidő (opcionális)" placeholder="óó:pp:mm" key={form.key('targetTime')} {...form.getInputProps('targetTime')} />
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
