import { Badge, Paper, SimpleGrid, Text } from '@mantine/core';
import type { ReactNode } from 'react';
import type { DashboardSummary } from '../../api/analytics/types.gen';
import { formatKm, formatPace, formatSeconds, riskCategories } from '../../lib/format';

/**
 * Egy összesítő kártya: felirat és érték.
 */
function StatCard({ label, value }: { label: string; value: ReactNode }) {
  return (
    <Paper withBorder p="md">
      <Text size="xs" c="dimmed" tt="uppercase" fw={700}>
        {label}
      </Text>
      <Text fw={700} size="xl" mt={4}>
        {value}
      </Text>
    </Paper>
  );
}

/**
 * Az időszak összesítő kártyái (spec 3.5): futott km, edzésidő, emelkedés, edzések száma,
 * átlagtempó és az aktuális kockázati pontszám.
 */
export function SummaryCards({ summary }: { summary: DashboardSummary }) {
  const category = summary.riskCategory ? riskCategories[summary.riskCategory] : null;

  return (
    <SimpleGrid cols={{ base: 2, sm: 3, lg: 6 }}>
      <StatCard label="Futott táv" value={formatKm(summary.distanceKm)} />
      <StatCard label="Edzésidő" value={formatSeconds(summary.durationSeconds)} />
      <StatCard label="Emelkedés" value={`${summary.elevationGainM} m`} />
      <StatCard label="Edzések" value={summary.workoutCount} />
      <StatCard label="Átlagtempó" value={formatPace(summary.avgPaceSecondsPerKm)} />
      <StatCard
        label="Kockázat"
        value={
          category ? (
            <>
              {summary.riskScore}{' '}
              <Badge color={category.color} variant="light" style={{ verticalAlign: 'middle' }}>
                {category.label}
              </Badge>
            </>
          ) : (
            '–'
          )
        }
      />
    </SimpleGrid>
  );
}
