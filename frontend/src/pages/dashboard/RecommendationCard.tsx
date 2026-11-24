import { Badge, Card, Group, Text, Title } from '@mantine/core';
import type { Recommendation } from '../../api/analytics/types.gen';
import { formatKm, workoutTypeLabels } from '../../lib/format';

/**
 * Az ajánlott következő edzés (UC10): típus, cél RPE-tartomány, hozzávetőleges időtartam és indoklás.
 */
export function RecommendationCard({ recommendation }: { recommendation: Recommendation | undefined }) {
  return (
    <Card withBorder>
      <Title order={4} mb="xs">
        Javasolt következő edzés
      </Title>
      {!recommendation ? (
        <Text c="dimmed">Betöltés…</Text>
      ) : (
        <>
          <Group gap="sm">
            <Badge size="lg" variant="light">
              {workoutTypeLabels[recommendation.type]}
            </Badge>
            <Text>
              RPE {recommendation.rpeMin === recommendation.rpeMax ? recommendation.rpeMin : `${recommendation.rpeMin}–${recommendation.rpeMax}`} · kb. {recommendation.durationMinutes} perc
              {recommendation.distanceKm ? ` · ${formatKm(recommendation.distanceKm)}` : ''}
            </Text>
          </Group>
          <Text size="sm" mt="sm">
            {recommendation.reason}
          </Text>
        </>
      )}
    </Card>
  );
}
