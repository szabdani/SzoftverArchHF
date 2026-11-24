import { Alert, Button, Card, Group, Loader, Stack, Text, Title } from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { errorMessage } from '../../api/apiError';
import {
  deleteApiIdentityLinksById,
  getApiIdentityInvitationsReceived,
  getApiIdentityLinks,
  postApiIdentityInvitationsByIdAccept,
  postApiIdentityInvitationsByIdDecline,
} from '../../api/identity/sdk.gen';
import { formatDate } from '../../lib/format';

/**
 * A futó edzői kapcsolatai (UC13): a kapott meghívók elfogadása vagy elutasítása, és a meglévő
 * kapcsolatok megszüntetése. Az elfogadás után az edző látja a futó adatait.
 */
export function CoachesPage() {
  const queryClient = useQueryClient();
  const received = useQuery({
    queryKey: ['invitations', 'received'],
    queryFn: async () => (await getApiIdentityInvitationsReceived({ throwOnError: true })).data,
  });
  const links = useQuery({
    queryKey: ['links'],
    queryFn: async () => (await getApiIdentityLinks({ throwOnError: true })).data,
  });

  /** Egy művelet a meghíváson vagy kapcsolaton; siker után frissül a két lista. */
  const action = useMutation({
    mutationFn: async ({ kind, id }: { kind: 'accept' | 'decline' | 'end'; id: string }) => {
      if (kind === 'accept') {
        await postApiIdentityInvitationsByIdAccept({ path: { id }, throwOnError: true });
      } else if (kind === 'decline') {
        await postApiIdentityInvitationsByIdDecline({ path: { id }, throwOnError: true });
      } else {
        await deleteApiIdentityLinksById({ path: { id }, throwOnError: true });
      }
    },
    onSuccess: async (_, { kind }) => {
      await queryClient.invalidateQueries();
      const messages = { accept: 'A meghívást elfogadtad.', decline: 'A meghívást elutasítottad.', end: 'A kapcsolat megszűnt.' };
      notifications.show({ color: 'teal', message: messages[kind] });
    },
    onError: (error) => notifications.show({ color: 'red', message: errorMessage(error) }),
  });

  const error = received.error ?? links.error;

  return (
    <Stack>
      <Title order={2}>Edzőim</Title>
      {error && <Alert color="red">{errorMessage(error)}</Alert>}
      {(received.isLoading || links.isLoading) && <Loader />}

      {(received.data ?? []).length > 0 && (
        <Card withBorder>
          <Title order={4} mb="xs">
            Meghívók
          </Title>
          <Stack gap="xs">
            {received.data!.map((invitation) => (
              <Group key={invitation.id} justify="space-between">
                <Text>
                  <b>{invitation.coachEmail}</b> edzőként meghívott ({formatDate(invitation.createdAt)}).
                </Text>
                <Group gap="xs">
                  <Button size="xs" onClick={() => action.mutate({ kind: 'accept', id: invitation.id })}>
                    Elfogadom
                  </Button>
                  <Button size="xs" variant="default" onClick={() => action.mutate({ kind: 'decline', id: invitation.id })}>
                    Elutasítom
                  </Button>
                </Group>
              </Group>
            ))}
          </Stack>
        </Card>
      )}

      <Card withBorder>
        <Title order={4} mb="xs">
          Edzőim
        </Title>
        {links.data?.length === 0 && <Text c="dimmed">Még nincs edződ. Ha egy edző meghív, itt fogadhatod el.</Text>}
        <Stack gap="xs">
          {(links.data ?? []).map((link) => (
            <Group key={link.id} justify="space-between">
              <Text>
                <b>{link.coachEmail}</b> · kapcsolat {formatDate(link.updatedAt)} óta
              </Text>
              <Button
                size="xs"
                color="red"
                variant="subtle"
                onClick={() => {
                  if (window.confirm(`Biztosan megszünteted a kapcsolatot ${link.coachEmail} edzővel? Utána nem látja az adataidat.`)) {
                    action.mutate({ kind: 'end', id: link.id });
                  }
                }}
              >
                Kapcsolat megszüntetése
              </Button>
            </Group>
          ))}
        </Stack>
      </Card>
    </Stack>
  );
}
