import { describe, expect, it } from 'vitest';
import type { CoachRunnerSummary } from '../../api/analytics/types.gen';
import type { InvitationResponse } from '../../api/identity/types.gen';
import { coachRunnerRows } from './coachRows';

/** Egy elfogadott kapcsolat a megadott futóval. */
function link(runnerId: string, runnerEmail: string): InvitationResponse {
  return { id: `l-${runnerId}`, coachId: 'c', coachEmail: 'coach@futoedzes.local', runnerEmail, runnerId, status: 'accepted', createdAt: '', updatedAt: '' };
}

const links = [link('r1', 'anna@pelda.hu'), link('r2', 'bela@pelda.hu'), link('r3', 'cecil@pelda.hu'), link('r4', 'aladar@pelda.hu')];
const summaries: CoachRunnerSummary[] = [
  { runnerId: 'r1', lastWorkoutDate: '2026-11-20', weeklyKm: 30, riskScore: 20, riskCategory: 'low' },
  { runnerId: 'r2', lastWorkoutDate: '2026-11-22', weeklyKm: 55, riskScore: 70, riskCategory: 'high' },
  { runnerId: 'r3', weeklyKm: 0 },
];

/** Az edző futóinak táblázata. */
describe('coachRunnerRows', () => {
  it('a kapcsolatokat az összesítővel fűzi össze, a pontszám nélküliek a végére kerülnek e-mail szerint', () => {
    const rows = coachRunnerRows(links, summaries, true);

    expect(rows.map((row) => row.email)).toEqual(['bela@pelda.hu', 'anna@pelda.hu', 'aladar@pelda.hu', 'cecil@pelda.hu']);
    expect(rows[0]).toEqual({ runnerId: 'r2', email: 'bela@pelda.hu', lastWorkoutDate: '2026-11-22', weeklyKm: 55, riskScore: 70, riskCategory: 'high' });
    // Az r4-ről még nincs összesítő (pl. most fogadta el a meghívót).
    expect(rows[2]).toEqual({ runnerId: 'r4', email: 'aladar@pelda.hu', lastWorkoutDate: null, weeklyKm: null, riskScore: null, riskCategory: null });
  });

  it('növekvő sorrendben is a pontszám nélküliek vannak hátul', () => {
    expect(coachRunnerRows(links, summaries, false).map((row) => row.runnerId)).toEqual(['r1', 'r2', 'r4', 'r3']);
  });
});
