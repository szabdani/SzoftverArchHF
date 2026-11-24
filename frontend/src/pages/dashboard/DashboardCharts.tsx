import { BarChart, CompositeChart, DonutChart, LineChart } from '@mantine/charts';
import { Card, SimpleGrid, Text, Title } from '@mantine/core';
import type { ReactNode } from 'react';
import type { IntensityDistribution, WeeklyStats } from '../../api/analytics/types.gen';
import { intensityCells, toWeeklyChartData } from './chartData';

/**
 * Egy diagram kártyán, címmel.
 */
function ChartCard({ title, children }: { title: string; children: ReactNode }) {
  return (
    <Card withBorder>
      <Title order={5} mb="sm">
        {title}
      </Title>
      {children}
    </Card>
  );
}

/**
 * A dashboard diagramjai (spec 3.5): heti km és terhelés ACWR-rel az utolsó 12 hétben,
 * intenzitáseloszlás az időszakban, terv és tény összevetése, a kockázati pontszám alakulása.
 */
export function DashboardCharts({ weeks, intensity }: { weeks: WeeklyStats[]; intensity: IntensityDistribution }) {
  const data = toWeeklyChartData(weeks);
  const cells = intensityCells(intensity);
  const totalMinutes = cells.reduce((sum, cell) => sum + cell.value, 0);

  return (
    <SimpleGrid cols={{ base: 1, md: 2 }}>
      <ChartCard title="Heti táv (km)">
        <BarChart h={220} data={data} dataKey="week" series={[{ name: 'km', label: 'Táv (km)', color: 'teal.6' }]} />
      </ChartCard>

      <ChartCard title="Heti terhelés és ACWR">
        <CompositeChart
          h={220}
          data={data}
          dataKey="week"
          withLegend
          withRightYAxis
          series={[
            { name: 'load', label: 'Heti terhelés (sRPE)', type: 'bar', color: 'blue.5' },
            { name: 'acwr', label: 'ACWR', type: 'line', color: 'red.6', yAxisId: 'right' },
          ]}
        />
      </ChartCard>

      <ChartCard title="Intenzitáseloszlás az időszakban (perc)">
        {totalMinutes === 0 ? (
          <Text c="dimmed">Ebben az időszakban még nincs edzés.</Text>
        ) : (
          <DonutChart h={220} data={cells} withLabels withLabelsLine chartLabel={`${totalMinutes} perc`} valueFormatter={(value) => `${value} perc`} />
        )}
      </ChartCard>

      <ChartCard title="Terv és tény (tervezett edzések hetente)">
        <BarChart
          h={220}
          data={data}
          dataKey="week"
          withLegend
          series={[
            { name: 'completed', label: 'Teljesített', color: 'teal.6' },
            { name: 'missed', label: 'Kihagyott', color: 'red.6' },
            { name: 'planned', label: 'Összes tervezett', color: 'gray.4' },
          ]}
        />
      </ChartCard>

      <ChartCard title="Kockázati pontszám alakulása (a hét végén)">
        <LineChart h={220} data={data} dataKey="week" yAxisProps={{ domain: [0, 100] }} series={[{ name: 'risk', label: 'Pontszám', color: 'orange.6' }]} />
      </ChartCard>
    </SimpleGrid>
  );
}
