import { Alert, Stack, Title } from '@mantine/core';
import { IconTools } from '@tabler/icons-react';

/**
 * Helykitöltő azoknak a nézeteknek, amelyekhez a backend már kész, de a felület még nem.
 */
export function NotReadyPage({ title }: { title: string }) {
  return (
    <Stack>
      <Title order={2}>{title}</Title>
      <Alert icon={<IconTools />} color="gray" title="Ez a nézet még készül">
        A funkció a backendben már elérhető, a felülete a következő lépésekben készül el.
      </Alert>
    </Stack>
  );
}
