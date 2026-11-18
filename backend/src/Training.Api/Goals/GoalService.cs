using System.Security.Claims;
using BuildingBlocks.Auth;
using BuildingBlocks.CoachAccess;
using BuildingBlocks.Time;
using Microsoft.EntityFrameworkCore;
using Training.Api.Persistence;
using Training.Api.Workouts;

namespace Training.Api.Goals;

/// <summary>
/// A célok üzleti logikája. Egy futónak egyszerre egy aktív célja lehet.
/// </summary>
public class GoalService(TrainingDbContext db, TimeProvider time) : IGoalService
{
    private const int RecentWorkoutCount = 5;

    /// <summary>
    /// Egy futó összes célja, az aktív elöl, utána a legutóbb kitűzöttek.
    /// </summary>
    /// <returns>A célok, vagy null, ha a felhasználó nem láthatja a futó adatait.</returns>
    public async Task<List<GoalResponse>?> ListAsync(ClaimsPrincipal user, Guid? runnerId, CancellationToken ct)
    {
        var targetRunnerId = runnerId ?? user.GetUserId();

        // Szerveroldali jogosultság-ellenőrzés: az edző csak hozzárendelt futó céljait láthatja.
        if (!await db.CanViewRunnerAsync(user, targetRunnerId, ct))
        {
            return null;
        }

        var goals = await db.Goals
            .Where(goal => goal.RunnerId == targetRunnerId)
            .OrderByDescending(goal => goal.IsActive)
            .ThenByDescending(goal => goal.CreatedAt)
            .ToListAsync(ct);

        return goals.Select(GoalResponse.From).ToList();
    }

    /// <summary>
    /// A célnézet: az aktív cél, a céldátumig hátralévő hetek és a legutóbbi edzések.
    /// </summary>
    /// <returns>A célnézet, vagy null, ha nincs aktív cél, vagy a felhasználó nem láthatja a futót.</returns>
    public async Task<ActiveGoalResponse?> GetActiveAsync(ClaimsPrincipal user, Guid? runnerId, CancellationToken ct)
    {
        var targetRunnerId = runnerId ?? user.GetUserId();
        if (!await db.CanViewRunnerAsync(user, targetRunnerId, ct))
        {
            return null;
        }

        var goal = await db.Goals.FirstOrDefaultAsync(goal => goal.RunnerId == targetRunnerId && goal.IsActive, ct);
        if (goal is null)
        {
            return null;
        }

        // A legutóbbi néhány edzés, a legfrissebb elöl.
        var recentWorkouts = await db.Workouts
            .Where(workout => workout.RunnerId == targetRunnerId)
            .OrderByDescending(workout => workout.Date)
            .ThenByDescending(workout => workout.CreatedAt)
            .Take(RecentWorkoutCount)
            .ToListAsync(ct);

        return new ActiveGoalResponse(
            GoalResponse.From(goal),
            Goal.WeeksUntil(goal.TargetDate, time.GetLocalToday()),
            recentWorkouts.Select(WorkoutResponse.From).ToList());
    }

    /// <summary>
    /// Új aktív cél kitűzése a bejelentkezett futónak; az előző aktív cél archiválódik.
    /// </summary>
    /// <returns>Az új cél.</returns>
    public async Task<GoalResponse> CreateAsync(ClaimsPrincipal user, ValidGoalInput input, CancellationToken ct)
    {
        var runnerId = user.GetUserId();
        var now = time.GetUtcNow();

        // Az előző aktív cél archiválása és az új felvétele egy SaveChanges-ben, egy tranzakcióban történik.
        var previousGoals = await db.Goals.Where(goal => goal.RunnerId == runnerId && goal.IsActive).ToListAsync(ct);
        foreach (var previousGoal in previousGoals)
        {
            previousGoal.Archive(now);
        }

        var newGoal = Goal.Create(runnerId, input, now);
        db.Goals.Add(newGoal);
        await db.SaveChangesAsync(ct);

        return GoalResponse.From(newGoal);
    }

    /// <summary>
    /// A futó saját aktív céljának módosítása. Archivált cél nem módosítható.
    /// </summary>
    /// <returns>A módosított cél, vagy null, ha nincs ilyen saját aktív cél.</returns>
    public async Task<GoalResponse?> UpdateAsync(ClaimsPrincipal user, Guid id, ValidGoalInput input, CancellationToken ct)
    {
        var runnerId = user.GetUserId();
        var goal = await db.Goals.FirstOrDefaultAsync(goal => goal.Id == id && goal.RunnerId == runnerId && goal.IsActive, ct);
        if (goal is null)
        {
            return null;
        }

        goal.Update(input);
        await db.SaveChangesAsync(ct);

        return GoalResponse.From(goal);
    }
}
