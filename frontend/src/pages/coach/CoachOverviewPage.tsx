import { Alert, Badge, Button, Card, Group, Loader, Stack, Table, Text, TextInput, Title, UnstyledButton } from '@mantine/core';
import { useForm } from '@mantine/form';
import { notifications } from '@mantine/notifications';
import { IconChevronDown, IconChevronUp, IconMail } from '@tabler/icons-react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { useNavigate } from 'react-router';
import { getApiAnalyticsCoachRunners } from '../../api/analytics/sdk.gen';
import { errorMessage } from '../../api/apiError';
import { getApiIdentityInvitationsSent, getApiIdentityLinks, postApiIdentityInvitations } from '../../api/identity/sdk.gen';
import type { InvitationStatus } from '../../api/identity/types.gen';
import { zInvitationRequest } from '../../api/identity/zod.gen';
import { formatDate, formatKm, riskCategories } from '../../lib/format';
import { schemaErrors } from '../../lib/schemaErrors';
import { applyServerErrors } from '../../lib/serverErrors';
import { coachRunnerRows } from './coachRows';

/** A meghívás állapotai: magyar név és szín. */
const invitationStatuses: Record<InvitationStatus, { label: string; color: string }> = {
  pending: { label: 'Függőben', color: 'yellow' },
  accepted: { label: 'Elfogadva', color: 'teal' },
  declined: { label: 'Elutasítva', color: 'red' },
  ended: { label: 'Megszűnt', color: 'gray' },
};

/**
 * Az edző nyitóoldala: futó meghívása e-mail címmel (UC12), az elküldött meghívók, és a hozzárendelt
 * futók utolsó edzéssel, heti km-rel és kockázati kategóriával, kockázat szerint rendezhetően (UC14).
 */
export function CoachOverviewPage() {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const [riskDescending, setRiskDescending] = useState(true);

  const links = useQuery({ queryKey: ['links'], queryFn: async () => (await getApiIdentityLinks({ throwOnError: true })).data });
  const summaries = useQuery({ queryKey: ['coach-runners'], queryFn: async () => (await getApiAnalyticsCoachRunners({ throwOnError: true })).data });
  const sent = useQuery({ queryKey: ['invitations', 'sent'], queryFn: async () => (await getApiIdentityInvitationsSent({ throwOnError: true })).data });

  const form = useForm({
    mode: 'controlled',
    initialValues: { email: '' },
    validate: (values) => schemaErrors(zInvitationRequest, { email: values.email.trim() }, { email: 'Adj meg egy érvényes e-mail címet.' }),
  });
  const invite = useMutation({
    mutationFn: (email: string) => postApiIdentityInvitations({ body: { email }, throwOnError: true }),
    onSuccess: async () => {
      form.reset();
      await queryClient.invalidateQueries({ queryKey: ['invitations'] });
      notifications.show({ color: 'teal', message: 'A meghívó elküldve; a futó a „Edzőim” oldalon fogadhatja el.' });
    },
    onError: (error) => {
      if (!applyServerErrors(form, error)) {
        notifications.show({ color: 'red', message: errorMessage(error) });
      }
    },
  });

  const rows = coachRunnerRows(links.data ?? [], summaries.data ?? [], riskDescending);
  const openInvitations = (sent.data ?? []).filter((invitation) => invitation.status !== 'accepted').slice(0, 10);
  const error = links.error ?? summaries.error ?? sent.error;

  return (
    <Stack>
      <Title order={2}>Futóim</Title>
      {error && <Alert color="red">{errorMessage(error)}</Alert>}

      <Card withBorder>
        <Title order={4} mb="xs">
          Futó meghívása
        </Title>
        <form onSubmit={form.onSubmit((values) => invite.mutate(values.email.trim()))}>
          <Group align="flex-start">
            <TextInput placeholder="futo@pelda.hu" aria-label="A futó e-mail címe" w={320} key={form.key('email')} {...form.getInputProps('email')} />
            <Button type="submit" leftSection={<IconMail size={16} />} loading={invite.isPending}>
              Meghívás
            </Button>
          </Group>
        </form>
        {openInvitations.length > 0 && (
          <Stack gap={4} mt="sm">
            {openInvitations.map((invitation) => (
              <Group key={invitation.id} gap="xs">
                <Badge color={invitationStatuses[invitation.status].color} variant="light">
                  {invitationStatuses[invitation.status].label}
                </Badge>
                <Text size="sm">
                  {invitation.runnerEmail} · {formatDate(invitation.createdAt)}
                </Text>
              </Group>
            ))}
          </Stack>
        )}
      </Card>

      {(links.isLoading || summaries.isLoading) && <Loader />}
      {links.isSuccess && rows.length === 0 && <Text c="dimmed">Még nincs hozzád rendelt futó. Hívj meg valakit a fenti mezővel.</Text>}

      {rows.length > 0 && (
        <Table.ScrollContainer minWidth={560}>
          <Table highlightOnHover>
            <Table.Thead>
              <Table.Tr>
                <Table.Th>Futó</Table.Th>
                <Table.Th>Utolsó edzés</Table.Th>
                <Table.Th>Heti táv</Table.Th>
                <Table.Th>
                  <UnstyledButton onClick={() => setRiskDescending(!riskDescending)}>
                    <Group gap={4} wrap="nowrap">
                      <Text fw={700} size="sm">
                        Kockázat
                      </Text>
                      {riskDescending ? <IconChevronDown size={14} /> : <IconChevronUp size={14} />}
                    </Group>
                  </UnstyledButton>
                </Table.Th>
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {rows.map((row) => (
                <Table.Tr key={row.runnerId} style={{ cursor: 'pointer' }} onClick={() => navigate(`/coach/runners/${row.runnerId}`)}>
                  <Table.Td>{row.email}</Table.Td>
                  <Table.Td>{row.lastWorkoutDate ? formatDate(row.lastWorkoutDate) : '–'}</Table.Td>
                  <Table.Td>{row.weeklyKm !== null ? formatKm(row.weeklyKm) : '–'}</Table.Td>
                  <Table.Td>
                    {row.riskCategory ? (
                      <Badge color={riskCategories[row.riskCategory].color}>
                        {row.riskScore} · {riskCategories[row.riskCategory].label}
                      </Badge>
                    ) : (
                      '–'
                    )}
                  </Table.Td>
                </Table.Tr>
              ))}
            </Table.Tbody>
          </Table>
        </Table.ScrollContainer>
      )}
    </Stack>
  );
}
