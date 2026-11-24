import type { RuleSettingsDto } from '../../api/analytics/types.gen';
import { zPutApiAnalyticsAdminRulesBody } from '../../api/analytics/zod.gen';
import { schemaErrors } from '../../lib/schemaErrors';

/** A beállítható szám mezők (a szabályok küszöbei). */
export type RuleNumberField = 'acwrElevated' | 'acwrCritical' | 'weeklyKmIncreasePercent' | 'intenseRpe' | 'intenseDaysPerWeek' | 'missedWorkoutsPerWeek';

/** Egy szabály az űrlapon: be/ki kapcsoló, leírás és a küszöbei. */
export type RuleGroup = {
  enabled: 'acwrEnabled' | 'weeklyKmIncreaseEnabled' | 'intenseDaysEnabled' | 'noRestDayEnabled' | 'missedWorkoutsEnabled';
  title: string;
  description: string;
  fields: { name: RuleNumberField; label: string; decimal?: boolean }[];
};

/** A kockázati és figyelmeztetési szabályok (spec 3.4–3.5) az űrlap sorrendjében. */
export const ruleGroups: RuleGroup[] = [
  {
    enabled: 'acwrEnabled',
    title: 'Hirtelen terhelésnövekedés (ACWR)',
    description: 'Az akut (7 napos) és a krónikus (28 napos) terhelés aránya.',
    fields: [
      { name: 'acwrElevated', label: 'Emelkedő terhelés ettől', decimal: true },
      { name: 'acwrCritical', label: 'Kritikus e fölött', decimal: true },
    ],
  },
  {
    enabled: 'weeklyKmIncreaseEnabled',
    title: 'Heti táv növekedése',
    description: 'Az előző héthez képest, százalékban.',
    fields: [{ name: 'weeklyKmIncreasePercent', label: 'Figyelmeztet e fölött (%)' }],
  },
  {
    enabled: 'intenseDaysEnabled',
    title: 'Túl sok intenzív nap',
    description: 'Intenzív napok 7 napon belül.',
    fields: [
      { name: 'intenseRpe', label: 'Intenzív ettől az RPE-től' },
      { name: 'intenseDaysPerWeek', label: 'Ennyi intenzív nap már sok' },
    ],
  },
  {
    enabled: 'noRestDayEnabled',
    title: 'Pihenőnap hiánya',
    description: '7 egymást követő edzésnap pihenő nélkül.',
    fields: [],
  },
  {
    enabled: 'missedWorkoutsEnabled',
    title: 'Kihagyott edzések',
    description: 'Kihagyott tervezett edzések 7 napon belül.',
    fields: [{ name: 'missedWorkoutsPerWeek', label: 'Ennyi kihagyott edzéstől' }],
  },
];

/** A mezők magyar hibaüzenetei; a tartományok a közös sémából jönnek. */
const messages: Record<RuleNumberField, string> = {
  acwrElevated: 'Az ACWR küszöb 0,5 és 5 között lehet.',
  acwrCritical: 'Az ACWR küszöb 0,5 és 5 között lehet.',
  weeklyKmIncreasePercent: 'A növekedés 1 és 100% között lehet.',
  intenseRpe: 'Az RPE 1 és 10 közötti egész szám lehet.',
  intenseDaysPerWeek: '2 és 7 közötti egész szám lehet.',
  missedWorkoutsPerWeek: '1 és 7 közötti egész szám lehet.',
};

/**
 * A szabálybeállítások ellenőrzése a szerverrel közös sémával, és hogy az emelkedő ACWR-sáv a kritikus
 * alatt kezdődjön (ezt a két mezőt összevető szabályt a szerver is ellenőrzi).
 */
export function validateRules(values: RuleSettingsDto): Record<string, string> {
  // A módosítás ideje csak tájékoztató, a szerver nem olvassa; ezért nem ellenőrizzük.
  const errors = schemaErrors(zPutApiAnalyticsAdminRulesBody, { ...values, updatedAt: null }, messages);

  if (!errors.acwrElevated && !errors.acwrCritical && values.acwrElevated >= values.acwrCritical) {
    errors.acwrElevated = 'Az emelkedő ACWR küszöbnek kisebbnek kell lennie a kritikusnál.';
  }

  return errors;
}
