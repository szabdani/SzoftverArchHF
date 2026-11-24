import { Alert, Button, Loader, Stack, Tabs, Title } from '@mantine/core';
import { IconArrowLeft } from '@tabler/icons-react';
import { useQuery } from '@tanstack/react-query';
import { Link, useParams } from 'react-router';
import { errorMessage } from '../../api/apiError';
import { getApiIdentityLinks } from '../../api/identity/sdk.gen';
import { CalendarPage } from '../calendar/CalendarPage';
import { DashboardPage } from '../dashboard/DashboardPage';
import { GoalsPage } from '../goals/GoalsPage';
import { WorkoutsPage } from '../workouts/WorkoutsPage';

/**
 * Egy hozzárendelt futó az edző szemével: a futó dashboardja, edzésnaplója és céljai (csak
 * megtekintés), valamint a naptára, ahol az edző edzéstervet készíthet neki (spec 3.1). A füleket csak
 * akkor mutatja, ha a futó valóban az edzőhöz tartozik.
 */
export function CoachRunnerPage() {
  const { runnerId } = useParams();
  const links = useQuery({ queryKey: ['links'], queryFn: async () => (await getApiIdentityLinks({ throwOnError: true })).data });
  const link = links.data?.find((candidate) => candidate.runnerId === runnerId);

  return (
    <Stack>
      <div>
        <Button component={Link} to="/coach" variant="subtle" leftSection={<IconArrowLeft size={16} />} px={0}>
          Futóim
        </Button>
      </div>
      <Title order={2}>{link?.runnerEmail ?? 'Futó'}</Title>

      {links.isLoading && <Loader />}
      {links.error && <Alert color="red">{errorMessage(links.error)}</Alert>}
      {links.isSuccess && !link && <Alert color="yellow">Ez a futó nincs hozzád rendelve, ezért az adatai nem láthatók.</Alert>}
      {link && (
        <Tabs defaultValue="dashboard" keepMounted={false}>
          <Tabs.List>
            <Tabs.Tab value="dashboard">Áttekintés</Tabs.Tab>
            <Tabs.Tab value="workouts">Edzésnapló</Tabs.Tab>
            <Tabs.Tab value="calendar">Naptár és tervek</Tabs.Tab>
            <Tabs.Tab value="goals">Célok</Tabs.Tab>
          </Tabs.List>
          <Tabs.Panel value="dashboard" pt="md">
            <DashboardPage runnerId={runnerId} />
          </Tabs.Panel>
          <Tabs.Panel value="workouts" pt="md">
            <WorkoutsPage runnerId={runnerId} readOnly />
          </Tabs.Panel>
          <Tabs.Panel value="calendar" pt="md">
            <CalendarPage runnerId={runnerId} />
          </Tabs.Panel>
          <Tabs.Panel value="goals" pt="md">
            <GoalsPage runnerId={runnerId} readOnly />
          </Tabs.Panel>
        </Tabs>
      )}
    </Stack>
  );
}
