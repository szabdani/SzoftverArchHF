import { Button, Group, Modal, Stack, Text } from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import dayjs from 'dayjs';
import { errorMessage } from '../../api/apiError';
import { deleteApiTrainingWorkoutsById } from '../../api/training/sdk.gen';
import type { WorkoutResponse } from '../../api/training/types.gen';
import { formatKm, workoutTypeLabels } from '../../lib/format';

/**
 * Edzés törlésének megerősítése (UC03); ha tervezett edzést teljesített, az újra tervezett lesz.
 */
export function DeleteWorkoutModal({ workout, onClose }: { workout: WorkoutResponse | null; onClose: () => void }) {
  const queryClient = useQueryClient();
  const mutation = useMutation({
    mutationFn: (id: string) => deleteApiTrainingWorkoutsById({ path: { id }, throwOnError: true }),
    onSuccess: async () => {
      await queryClient.invalidateQueries();
      notifications.show({ color: 'teal', message: 'Az edzés törölve.' });
      onClose();
    },
    onError: (error) => notifications.show({ color: 'red', message: errorMessage(error) }),
  });

  return (
    <Modal opened={workout !== null} onClose={onClose} title="Edzés törlése">
      {workout && (
        <Stack>
          <Text>
            Biztosan törlöd a {dayjs(workout.date).format('YYYY. MM. DD.')} napi {workoutTypeLabels[workout.type].toLowerCase()} edzést ({formatKm(workout.distanceKm)})?
          </Text>
          <Group justify="flex-end">
            <Button variant="default" onClick={onClose}>
              Mégse
            </Button>
            <Button color="red" loading={mutation.isPending} onClick={() => mutation.mutate(workout.id)}>
              Törlés
            </Button>
          </Group>
        </Stack>
      )}
    </Modal>
  );
}
