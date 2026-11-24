import { Alert, Button, Card, Group, Loader, NumberInput, Stack, Switch, Text, Title } from '@mantine/core';
import { useForm } from '@mantine/form';
import { notifications } from '@mantine/notifications';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import dayjs from 'dayjs';
import { getApiAnalyticsAdminRules, putApiAnalyticsAdminRules } from '../../api/analytics/sdk.gen';
import type { RuleSettingsDto } from '../../api/analytics/types.gen';
import { errorMessage } from '../../api/apiError';
import { applyServerErrors } from '../../lib/serverErrors';
import { ruleGroups, validateRules } from './rulesForm';

/**
 * A kockázati és figyelmeztetési szabályok beállítása (UC17): küszöbök és be/ki kapcsolás. Mentés után
 * a futók mai pontszáma azonnal az új küszöbökkel számolódik újra.
 */
export function AdminRulesPage() {
  const rules = useQuery({ queryKey: ['admin-rules'], queryFn: async () => (await getApiAnalyticsAdminRules({ throwOnError: true })).data });

  return (
    <Stack>
      <Title order={2}>Szabályok</Title>
      <Text c="dimmed">A kockázati pontszám és a figyelmeztetések küszöbei. A kikapcsolt szabály nem számít bele a pontszámba, és nem küld figyelmeztetést.</Text>
      {rules.isLoading && <Loader />}
      {rules.error && <Alert color="red">{errorMessage(rules.error)}</Alert>}
      {/* Mentés után új módosítási idővel jön vissza, így az űrlap a mentett értékekkel indul újra. */}
      {rules.data && <RulesForm key={rules.data.updatedAt ?? 'alap'} rules={rules.data} />}
    </Stack>
  );
}

/**
 * A szabályok űrlapja a betöltött beállításokkal.
 */
function RulesForm({ rules }: { rules: RuleSettingsDto }) {
  const queryClient = useQueryClient();
  const form = useForm({ mode: 'controlled', initialValues: rules, validate: validateRules });
  const save = useMutation({
    mutationFn: (body: RuleSettingsDto) => putApiAnalyticsAdminRules({ body, throwOnError: true }),
    onSuccess: async () => {
      await queryClient.invalidateQueries();
      notifications.show({ color: 'teal', message: 'A szabályok elmentve, a kockázati pontszámok frissültek.' });
    },
    onError: (error) => {
      if (!applyServerErrors(form, error)) {
        notifications.show({ color: 'red', message: errorMessage(error) });
      }
    },
  });

  return (
    <form onSubmit={form.onSubmit((values) => save.mutate(values))}>
      <Stack>
        {ruleGroups.map((group) => (
          <Card key={group.enabled} withBorder>
            <Switch label={group.title} description={group.description} key={form.key(group.enabled)} {...form.getInputProps(group.enabled, { type: 'checkbox' })} />
            {group.fields.length > 0 && (
              <Group mt="sm" align="flex-start">
                {group.fields.map((field) => (
                  <NumberInput
                    key={form.key(field.name)}
                    label={field.label}
                    w={220}
                    decimalSeparator=","
                    decimalScale={field.decimal ? 2 : 0}
                    step={field.decimal ? 0.05 : 1}
                    disabled={!form.values[group.enabled]}
                    {...form.getInputProps(field.name)}
                  />
                ))}
              </Group>
            )}
          </Card>
        ))}
        <Group>
          <Button type="submit" loading={save.isPending}>
            Mentés
          </Button>
          <Text size="sm" c="dimmed">
            {rules.updatedAt ? `Utoljára módosítva: ${dayjs(rules.updatedAt).format('YYYY. MM. DD. HH:mm')}` : 'A spec alapértelmezései vannak érvényben.'}
          </Text>
        </Group>
      </Stack>
    </form>
  );
}
