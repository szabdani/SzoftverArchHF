using System.Security.Claims;
using Analytics.Api.Persistence;
using BuildingBlocks.Auth;
using BuildingBlocks.CoachAccess;
using BuildingBlocks.Time;
using Microsoft.EntityFrameworkCore;

namespace Analytics.Api.Dashboard;

/// <summary>
/// Összegyűjti a dashboard adatait a helyi másolatból; a számítás a <see cref="DashboardCalculator"/>-ban van.
/// </summary>
public class DashboardService(AnalyticsDbContext db, TimeProvider time) : IDashboardService
{
    /// <summary>
    /// A futó dashboardja a heti vagy havi időszakra.
    /// </summary>
    /// <returns>A dashboard, vagy null, ha a felhasználó nem láthatja a futó adatait.</returns>
    public async Task<DashboardResponse?> GetAsync(ClaimsPrincipal user, Guid? runnerId, DashboardPeriod period, CancellationToken ct)
    {
        var targetRunnerId = runnerId ?? user.GetUserId();

        // A futó a sajátját, a hozzárendelt edző a futóét látja; az admin nem (spec 3.1).
        if (!await db.CanViewRunnerAsync(user, targetRunnerId, ct))
        {
            return null;
        }

        var today = time.GetLocalToday();
        var historyStart = today.AddDays(-DashboardCalculator.RequiredHistoryDays);
        var firstWeekStart = DashboardCalculator.WeekStart(today).AddDays(-7 * (DashboardCalculator.WeekCount - 1));
        var currentWeekEnd = DashboardCalculator.WeekStart(today).AddDays(6);

        // Edzések a 12 hétre és az első hét krónikus terheléséhez szükséges előzményre.
        var workouts = await db.Workouts
            .Where(workout => workout.RunnerId == targetRunnerId && workout.Date >= historyStart && workout.Date <= today)
            .ToListAsync(ct);

        // Tervezett edzések a 12 hétre, a folyó hét végéig (a terv–tény összevetéshez).
        var plannedWorkouts = await db.PlannedWorkouts
            .Where(planned => planned.RunnerId == targetRunnerId && planned.Date >= firstWeekStart.AddDays(-6) && planned.Date <= currentWeekEnd)
            .ToListAsync(ct);

        var firstWorkoutDate = await db.Workouts
            .Where(workout => workout.RunnerId == targetRunnerId)
            .MinAsync(workout => (DateOnly?)workout.Date, ct);

        var currentRisk = await db.RiskScoreSnapshots
            .Where(snapshot => snapshot.RunnerId == targetRunnerId && snapshot.Date <= today)
            .OrderByDescending(snapshot => snapshot.Date)
            .FirstOrDefaultAsync(ct);

        return DashboardCalculator.Calculate(period, workouts, plannedWorkouts, today, firstWorkoutDate, currentRisk);
    }

    /// <summary>
    /// Az edzőhöz rendelt futók kockázat szerint csökkenő sorrendben (pontszám nélküliek a végén).
    /// </summary>
    public async Task<List<CoachRunnerSummary>> ListCoachRunnersAsync(ClaimsPrincipal coach, CancellationToken ct)
    {
        var coachId = coach.GetUserId();
        var today = time.GetLocalToday();
        var weekStart = DashboardCalculator.WeekStart(today);

        var runnerIds = await db.CoachRunnerLinks
            .Where(link => link.CoachId == coachId)
            .Select(link => link.RunnerId)
            .ToListAsync(ct);

        // Futónként néhány egyszerű lekérdezés; egy edzőnek kevés futója van.
        var summaries = new List<CoachRunnerSummary>();
        foreach (var runnerId in runnerIds)
        {
            var lastWorkoutDate = await db.Workouts
                .Where(workout => workout.RunnerId == runnerId)
                .MaxAsync(workout => (DateOnly?)workout.Date, ct);

            var weeklyKm = await db.Workouts
                .Where(workout => workout.RunnerId == runnerId && workout.Date >= weekStart && workout.Date <= today)
                .SumAsync(workout => workout.DistanceKm, ct);

            var risk = await db.RiskScoreSnapshots
                .Where(snapshot => snapshot.RunnerId == runnerId && snapshot.Date <= today)
                .OrderByDescending(snapshot => snapshot.Date)
                .FirstOrDefaultAsync(ct);

            summaries.Add(new CoachRunnerSummary(runnerId, lastWorkoutDate, weeklyKm, risk?.Score, risk?.Category));
        }

        return summaries
            .OrderByDescending(summary => summary.RiskScore ?? -1)
            .ThenByDescending(summary => summary.LastWorkoutDate)
            .ToList();
    }
}
