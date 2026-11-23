import { Alert, Card, Group, Loader, SegmentedControl, SimpleGrid, Stack, Title } from '@mantine/core';
import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { getApiAnalyticsAlerts, getApiAnalyticsDashboard, getApiAnalyticsRecommendation, getApiAnalyticsRisk } from '../../api/analytics/sdk.gen';
import type { DashboardPeriod } from '../../api/analytics/types.gen';
import { errorMessage } from '../../api/apiError';
import { AlertList } from '../alerts/AlertList';
import { DashboardCharts } from './DashboardCharts';
import { RecommendationCard } from './RecommendationCard';
import { RiskCard } from './RiskCard';
import { SummaryCards } from './SummaryCards';

/**
 * A futó terhelési dashboardja (UC06): heti/havi összesítő, kockázati pontszám (UC15), javasolt
 * következő edzés (UC10), nyugtázatlan figyelmeztetések (UC09) és a 12 hetes diagramok.
 */
export function DashboardPage() {
  const [period, setPeriod] = useState<DashboardPeriod>('week');

  const dashboard = useQuery({
    queryKey: ['dashboard', period],
    queryFn: async () => (await getApiAnalyticsDashboard({ query: { period }, throwOnError: true })).data,
  });
  const risk = useQuery({
    queryKey: ['risk'],
    queryFn: async () => (await getApiAnalyticsRisk({ throwOnError: true })).data,
  });
  const recommendation = useQuery({
    queryKey: ['recommendation'],
    queryFn: async () => (await getApiAnalyticsRecommendation({ throwOnError: true })).data,
  });
  const alerts = useQuery({
    queryKey: ['alerts', false],
    queryFn: async () => (await getApiAnalyticsAlerts({ query: { includeAcknowledged: false }, throwOnError: true })).data,
  });

  const error = dashboard.error ?? risk.error ?? recommendation.error ?? alerts.error;

  return (
    <Stack>
      <Group justify="space-between">
        <Title order={2}>Áttekintés</Title>
        <SegmentedControl
          value={period}
          onChange={(value) => setPeriod(value as DashboardPeriod)}
          data={[
            { value: 'week', label: 'Heti' },
            { value: 'month', label: 'Havi' },
          ]}
        />
      </Group>

      {error && <Alert color="red">{errorMessage(error)}</Alert>}
      {dashboard.isLoading && <Loader />}
      {dashboard.data && <SummaryCards summary={dashboard.data.summary} />}

      <SimpleGrid cols={{ base: 1, md: 2 }}>
        <RiskCard risk={risk.data?.current} />
        <RecommendationCard recommendation={recommendation.data} />
      </SimpleGrid>

      <Card withBorder>
        <Title order={4} mb="xs">
          Figyelmeztetések
        </Title>
        {alerts.data && <AlertList alerts={alerts.data} />}
      </Card>

      {dashboard.data && <DashboardCharts weeks={dashboard.data.weeks} intensity={dashboard.data.intensity} />}
    </Stack>
  );
}
