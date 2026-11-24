import type { CoachRunnerSummary, RiskCategory } from '../../api/analytics/types.gen';
import type { InvitationResponse } from '../../api/identity/types.gen';

/** Egy futó az edző áttekintő táblázatában. */
export type CoachRunnerRow = {
  runnerId: string;
  email: string;
  lastWorkoutDate: string | null;
  weeklyKm: number | null;
  riskScore: number | null;
  riskCategory: RiskCategory | null;
};

/**
 * Az edző futói (UC14): a kapcsolatok (Identity, a futó e-mail címével) és a terhelési összesítő
 * (Analytics) futónként összefűzve, kockázat szerint rendezve. A még pontszám nélküli futók mindig a
 * lista végére kerülnek, e-mail szerint.
 */
export function coachRunnerRows(links: InvitationResponse[], summaries: CoachRunnerSummary[], riskDescending: boolean): CoachRunnerRow[] {
  const summaryByRunner = new Map(summaries.map((summary) => [summary.runnerId, summary]));

  return links
    .filter((link) => link.runnerId)
    .map((link) => {
      const summary = summaryByRunner.get(link.runnerId!);
      return {
        runnerId: link.runnerId!,
        email: link.runnerEmail,
        lastWorkoutDate: summary?.lastWorkoutDate ?? null,
        weeklyKm: summary?.weeklyKm ?? null,
        riskScore: summary?.riskScore ?? null,
        riskCategory: summary?.riskCategory ?? null,
      };
    })
    .toSorted((a, b) => {
      if (a.riskScore === null || b.riskScore === null) {
        return a.riskScore === b.riskScore ? a.email.localeCompare(b.email) : a.riskScore === null ? 1 : -1;
      }

      return riskDescending ? b.riskScore - a.riskScore : a.riskScore - b.riskScore;
    });
}
