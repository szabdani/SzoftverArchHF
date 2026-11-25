import type { ReactNode } from 'react';
import { Group, Input, NumberInput } from '@mantine/core';
import { formatSeconds } from './format';

/** Egy időtartam-mező belső állapota: külön óra, perc és másodperc (üres = még nincs megadva). */
type DurationParts = { hours: number | string; minutes: number | string; seconds: number | string };

/**
 * Az időtartam óó:pp:mm stringjét a három mezőre bontja; érvénytelen vagy üres értéknél üres mezők.
 */
function splitDuration(value: string): DurationParts {
  const match = /^(\d{1,2}):([0-5]?\d):([0-5]?\d)$/.exec(value.trim());
  if (!match) {
    return { hours: '', minutes: '', seconds: '' };
  }

  return { hours: Number(match[1]), minutes: Number(match[2]), seconds: Number(match[3]) };
}

/**
 * A három mezőből a form által várt óó:pp:mm string; ha mindhárom üres, üres stringet ad (így a
 * „kötelező” hiba is megjelenhet). Bármelyik kitöltött mező esetén a hiányzókat 0-nak veszi.
 */
function joinDuration({ hours, minutes, seconds }: DurationParts): string {
  if (hours === '' && minutes === '' && seconds === '') {
    return '';
  }

  const total = Number(hours || 0) * 3600 + Number(minutes || 0) * 60 + Number(seconds || 0);
  return formatSeconds(total);
}

/**
 * Időtartam bevitele három külön, fel-le nyíllal állítható mezővel (óra / perc / másodperc), a korábbi
 * óó:pp:mm szöveges mező helyett. Kifelé ugyanazt az óó:pp:mm stringet adja, így a formok és a szerverrel
 * közös validáció változatlan. A tartományhatárt (pl. 1 perc–10 óra) a hívó oldali séma adja, ezért a
 * felső korlát itt nincs: a túllépés submitkor mezőnkénti hibaüzenetként jelenik meg. A perc/mp 0–59
 * korlátja a formátum része (60 perc = 1 óra).
 */
export function DurationInput({
  label,
  withAsterisk,
  value,
  onChange,
  error,
}: {
  label: string;
  withAsterisk?: boolean;
  value: string;
  onChange: (next: string) => void;
  error?: ReactNode;
}) {
  const parts = splitDuration(value);
  const update = (next: Partial<DurationParts>) => onChange(joinDuration({ ...parts, ...next }));

  return (
    <Input.Wrapper label={label} withAsterisk={withAsterisk} error={error}>
      <Group grow gap="xs" wrap="nowrap">
        <NumberInput aria-label="Óra" placeholder="óra" min={0} allowDecimal={false} value={parts.hours} onChange={(v) => update({ hours: v })} />
        <NumberInput aria-label="Perc" placeholder="perc" min={0} max={59} allowDecimal={false} value={parts.minutes} onChange={(v) => update({ minutes: v })} />
        <NumberInput aria-label="Másodperc" placeholder="mp" min={0} max={59} allowDecimal={false} value={parts.seconds} onChange={(v) => update({ seconds: v })} />
      </Group>
    </Input.Wrapper>
  );
}
