import { Badge, Card, Group, List, Text, Title } from '@mantine/core';
import type { RiskScoreResponse } from '../../api/analytics/types.gen';
import { riskCategories } from '../../lib/format';

/**
 * Az aktuális kockázati pontszám (UC15): pontszám, kategória, ACWR és a teljesült szabályok indoklása.
 * A pontszám tájékoztató jellegű, nem orvosi diagnózis (spec 3.8).
 */
export function RiskCard({ risk }: { risk: RiskScoreResponse | null | undefined }) {
  return (
    <Card withBorder>
      <Title order={4} mb="xs">
        Túlterhelési kockázat
      </Title>
      {!risk ? (
        <Text c="dimmed">Még nincs elég adat a pontszámhoz.</Text>
      ) : (
        <>
          <Group gap="sm">
            <Text fw={700} size="xl">
              {risk.score} / 100
            </Text>
            <Badge color={riskCategories[risk.category].color}>{riskCategories[risk.category].label}</Badge>
            <Text size="sm" c="dimmed">
              ACWR: {risk.acwr?.toLocaleString('hu-HU') ?? '–'}
            </Text>
          </Group>
          {risk.reasons.length > 0 ? (
            <List size="sm" mt="sm">
              {risk.reasons.map((reason) => (
                <List.Item key={reason}>{reason}</List.Item>
              ))}
            </List>
          ) : (
            <Text size="sm" mt="sm" c="dimmed">
              Egyik kockázati szabály sem teljesül.
            </Text>
          )}
          <Text size="xs" c="dimmed" mt="sm">
            Tájékoztató jellegű pontszám, nem orvosi diagnózis.
          </Text>
        </>
      )}
    </Card>
  );
}
