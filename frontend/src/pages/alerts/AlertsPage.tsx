import { Alert, Group, Loader, Stack, Switch, Title } from '@mantine/core';
import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { getApiAnalyticsAlerts } from '../../api/analytics/sdk.gen';
import { errorMessage } from '../../api/apiError';
import { AlertList } from './AlertList';

/**
 * A futó figyelmeztetései (UC09); kérésre a már nyugtázottak is.
 */
export function AlertsPage() {
  const [includeAcknowledged, setIncludeAcknowledged] = useState(false);
  const query = useQuery({
    queryKey: ['alerts', includeAcknowledged],
    queryFn: async () => (await getApiAnalyticsAlerts({ query: { includeAcknowledged }, throwOnError: true })).data,
  });

  return (
    <Stack>
      <Group justify="space-between">
        <Title order={2}>Figyelmeztetések</Title>
        <Switch label="Nyugtázottak is" checked={includeAcknowledged} onChange={(event) => setIncludeAcknowledged(event.currentTarget.checked)} />
      </Group>
      {query.isError && <Alert color="red">{errorMessage(query.error)}</Alert>}
      {query.isLoading && <Loader />}
      {query.data && <AlertList alerts={query.data} />}
    </Stack>
  );
}
