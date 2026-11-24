import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useEffect } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '../../api/apiError';
import * as sdk from '../../api/training/sdk.gen';
import type { PlannedWorkoutResponse } from '../../api/training/types.gen';
import { renderWithProviders } from '../../test/render';
import { CalendarPage } from './CalendarPage';

const item: PlannedWorkoutResponse = { id: 'pw1', planId: 'p1', runnerId: 'r', date: '2026-11-25', type: 'tempo', plannedDistanceKm: 10, plannedDuration: null, targetRpe: 7, note: null, status: 'planned' };
const revert = vi.fn();

// A FullCalendar helyett egy pótlék: induláskor jelzi a látható időszakot, a gombbal pedig áthelyezést szimulál.
vi.mock('@fullcalendar/react', () => ({
  default: function FullCalendarStub(props: { datesSet: (info: { start: Date; end: Date }) => void; eventDrop: (info: unknown) => void; events: { title: string }[] }) {
    useEffect(() => props.datesSet({ start: new Date('2026-10-26T00:00:00'), end: new Date('2026-12-07T00:00:00') }), []); // eslint-disable-line react-hooks/exhaustive-deps
    return (
      <div>
        {props.events.map((event) => (
          <span key={event.title}>{event.title}</span>
        ))}
        <button onClick={() => props.eventDrop({ event: { start: new Date('2026-11-27T00:00:00'), extendedProps: { planned: item } }, revert })}>húzás</button>
      </div>
    );
  },
}));
vi.mock('@fullcalendar/react/daygrid', () => ({ default: {} }));
vi.mock('@fullcalendar/react/interaction', () => ({ default: {} }));
vi.mock('@fullcalendar/react/locales/hu', () => ({ default: {} }));
vi.mock('@fullcalendar/react/themes/classic', () => ({ default: {} }));
vi.mock('../../api/training/sdk.gen', () => ({
  getApiTrainingPlans: vi.fn(),
  getApiTrainingPlannedWorkouts: vi.fn(),
  getApiTrainingWorkouts: vi.fn(),
  putApiTrainingPlansByPlanIdWorkoutsById: vi.fn(),
  deleteApiTrainingPlansById: vi.fn(),
  postApiTrainingPlans: vi.fn(),
  putApiTrainingPlansById: vi.fn(),
  postApiTrainingPlansByPlanIdWorkouts: vi.fn(),
  deleteApiTrainingPlansByPlanIdWorkoutsById: vi.fn(),
}));

/** A naptár: a látható időszak adatai és az áthelyezés húzással. */
describe('CalendarPage', () => {
  beforeEach(() => {
    revert.mockClear();
    vi.mocked(sdk.getApiTrainingPlans).mockResolvedValue({ data: [{ id: 'p1', runnerId: 'r', createdById: 'r', name: 'Alapozás', startDate: '2026-11-23', endDate: '2026-12-20', createdAt: '', goalId: 'g1', workouts: [item] }] } as never);
    vi.mocked(sdk.getApiTrainingPlannedWorkouts).mockResolvedValue({ data: [item] } as never);
    vi.mocked(sdk.getApiTrainingWorkouts).mockResolvedValue({ data: [] } as never);
  });

  it('a látható időszakra kéri a tervezett edzéseket, és megmutatja a terveket', async () => {
    renderWithProviders(<CalendarPage runnerId="runner-1" />);

    expect(await screen.findByText('Tempó · 10 km · RPE 7')).toBeInTheDocument();
    expect(sdk.getApiTrainingPlannedWorkouts).toHaveBeenCalledWith(expect.objectContaining({ query: { runnerId: 'runner-1', from: '2026-10-26', to: '2026-12-06' } }));
    expect(screen.getByText('Alapozás')).toBeInTheDocument();
    expect(screen.getByText('célhoz kötve')).toBeInTheDocument();
  });

  it('áthelyezéskor az új dátummal menti; ha a szerver elutasítja, visszaugrik', async () => {
    vi.mocked(sdk.putApiTrainingPlansByPlanIdWorkoutsById).mockRejectedValueOnce(new ApiError(400, { errors: { date: ['A dátumnak a terv időszakába kell esnie.'] } }));
    renderWithProviders(<CalendarPage />);

    await userEvent.click(await screen.findByRole('button', { name: 'húzás' }));

    await waitFor(() => expect(revert).toHaveBeenCalled());
    expect(vi.mocked(sdk.putApiTrainingPlansByPlanIdWorkoutsById).mock.calls[0][0]).toMatchObject({
      path: { planId: 'p1', id: 'pw1' },
      body: { date: '2026-11-27', type: 'tempo', plannedDistanceKm: 10, targetRpe: 7 },
    });
  });
});
