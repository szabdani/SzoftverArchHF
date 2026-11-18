using System.Security.Claims;
using Analytics.Api.Persistence;
using BuildingBlocks.Auth;
using BuildingBlocks.CoachAccess;
using BuildingBlocks.Time;
using Contracts.Training;
using Microsoft.EntityFrameworkCore;

namespace Analytics.Api.Recommendations;

/// <summary>
/// Összegyűjti a javaslathoz szükséges adatokat, és a <see cref="RecommendationRules"/> szerint dönt.
/// A javaslat kérésre számolódik, nem tárolódik.
/// </summary>
public class RecommendationService(AnalyticsDbContext db, TimeProvider time) : IRecommendationService
{
    /// <summary>
    /// A javaslat a mai napra.
    /// </summary>
    /// <returns>A javaslat, vagy null, ha a felhasználó nem láthatja a futó adatait.</returns>
    public async Task<Recommendation?> GetAsync(ClaimsPrincipal user, Guid? runnerId, CancellationToken ct)
    {
        var targetRunnerId = runnerId ?? user.GetUserId();
        if (!await db.CanViewRunnerAsync(user, targetRunnerId, ct))
        {
            return null;
        }

        var today = time.GetLocalToday();

        // A legutóbbi kockázati kategória (a mai, ha már van).
        var riskCategory = await db.RiskScoreSnapshots
            .Where(snapshot => snapshot.RunnerId == targetRunnerId && snapshot.Date <= today)
            .OrderByDescending(snapshot => snapshot.Date)
            .Select(snapshot => (Risk.RiskCategory?)snapshot.Category)
            .FirstOrDefaultAsync(ct);

        // Az utolsó 14 nap edzései: ebben benne van a hét eleje és az utolsó intenzív nap keresése is.
        var recentWorkouts = await db.Workouts
            .Where(workout => workout.RunnerId == targetRunnerId && workout.Date >= today.AddDays(-13) && workout.Date <= today)
            .ToListAsync(ct);

        var plannedToday = await db.PlannedWorkouts.FirstOrDefaultAsync(plannedWorkout =>
            plannedWorkout.RunnerId == targetRunnerId
            && plannedWorkout.Date == today
            && plannedWorkout.Status == PlannedWorkoutStatus.Planned, ct);

        var goal = await db.ActiveGoals.FirstOrDefaultAsync(goal => goal.RunnerId == targetRunnerId, ct);

        return RecommendationRules.Recommend(riskCategory, recentWorkouts, plannedToday, goal?.Type, goal?.DistanceKm, today);
    }
}
