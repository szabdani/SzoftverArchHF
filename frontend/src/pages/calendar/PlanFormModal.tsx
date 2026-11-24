import { Button, Group, Modal, Stack, Switch, TextInput } from '@mantine/core';
import { DateInput } from '@mantine/dates';
import { useForm } from '@mantine/form';
import { notifications } from '@mantine/notifications';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { errorMessage } from '../../api/apiError';
import { postApiTrainingPlans, putApiTrainingPlansById } from '../../api/training/sdk.gen';
import type { PlanRequest, PlanResponse } from '../../api/training/types.gen';
import { applyServerErrors } from '../../lib/serverErrors';
import { planToFormValues, toPlanRequest, validatePlan } from './planForms';

/**
 * Edzésterv létrehozása vagy módosítása (UC11): név, időszak, aktív célhoz kötés. Az edző a futó
 * azonosítójával (runnerId) a futónak készíti.
 */
export function PlanFormModal({ onClose, plan, runnerId }: { onClose: () => void; plan?: PlanResponse; runnerId?: string }) {
  const queryClient = useQueryClient();
  const form = useForm({ mode: 'controlled', initialValues: planToFormValues(plan), validate: validatePlan });

  const mutation = useMutation({
    mutationFn: (body: PlanRequest) =>
      plan ? putApiTrainingPlansById({ path: { id: plan.id }, body, throwOnError: true }) : postApiTrainingPlans({ body, throwOnError: true }),
    onSuccess: async () => {
      await queryClient.invalidateQueries();
      notifications.show({ color: 'teal', message: plan ? 'A terv módosítva.' : 'A terv elkészült.' });
      onClose();
    },
    onError: (error) => {
      if (!applyServerErrors(form, error)) {
        notifications.show({ color: 'red', message: errorMessage(error) });
      }
    },
  });

  return (
    <Modal opened onClose={onClose} title={plan ? 'Terv módosítása' : 'Új edzésterv'}>
      <form onSubmit={form.onSubmit((values) => mutation.mutate(toPlanRequest(values, runnerId)))}>
        <Stack>
          <TextInput label="Név" withAsterisk key={form.key('name')} {...form.getInputProps('name')} />
          <Group grow>
            <DateInput label="Kezdődátum" withAsterisk valueFormat="YYYY. MM. DD." key={form.key('startDate')} {...form.getInputProps('startDate')} />
            <DateInput label="Záródátum" withAsterisk valueFormat="YYYY. MM. DD." key={form.key('endDate')} {...form.getInputProps('endDate')} />
          </Group>
          <Switch label="Az aktív célhoz kötöm" key={form.key('linkActiveGoal')} {...form.getInputProps('linkActiveGoal', { type: 'checkbox' })} />
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
