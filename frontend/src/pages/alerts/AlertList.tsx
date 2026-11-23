import { Alert, Button, Group, Stack, Text } from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import dayjs from 'dayjs';
import { postApiAnalyticsAlertsByIdAcknowledge } from '../../api/analytics/sdk.gen';
import type { AlertResponse } from '../../api/analytics/types.gen';
import { errorMessage } from '../../api/apiError';
import { alertSeverities } from '../../lib/format';

/**
 * Figyelmeztetések listája (UC09) a szerver sorrendjében (súlyosság, majd időpont szerint);
 * a nyugtázatlanok nyugtázhatók.
 */
export function AlertList({ alerts }: { alerts: AlertResponse[] }) {
  const queryClient = useQueryClient();
  const acknowledge = useMutation({
    mutationFn: (id: string) => postApiAnalyticsAlertsByIdAcknowledge({ path: { id }, throwOnError: true }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['alerts'] }),
    onError: (error) => notifications.show({ color: 'red', message: errorMessage(error) }),
  });

  if (alerts.length === 0) {
    return <Text c="dimmed">Nincs figyelmeztetés.</Text>;
  }

  return (
    <Stack gap="xs">
      {alerts.map((alert) => {
        const severity = alertSeverities[alert.severity];
        return (
          <Alert key={alert.id} color={severity.color} title={severity.label} variant={alert.acknowledgedAt ? 'outline' : 'light'}>
            <Group justify="space-between" wrap="nowrap" align="flex-start">
              <div>
                <Text size="sm">{alert.message}</Text>
                <Text size="xs" c="dimmed">
                  {dayjs(alert.createdAt).format('YYYY. MM. DD. HH:mm')}
                  {alert.acknowledgedAt && ` · nyugtázva: ${dayjs(alert.acknowledgedAt).format('YYYY. MM. DD.')}`}
                </Text>
              </div>
              {!alert.acknowledgedAt && (
                <Button size="xs" variant="default" loading={acknowledge.isPending && acknowledge.variables === alert.id} onClick={() => acknowledge.mutate(alert.id)}>
                  Nyugtázom
                </Button>
              )}
            </Group>
          </Alert>
        );
      })}
    </Stack>
  );
}
