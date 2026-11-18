using System.Security.Claims;
using BuildingBlocks.Auth;
using BuildingBlocks.CoachAccess;
using BuildingBlocks.Time;
using Contracts.Training;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Training.Api.Persistence;

namespace Training.Api.Plans;

/// <summary>
/// Az edzéstervek üzleti logikája. Minden tervezett edzés változásáról eseményt küld, amiből az
/// Analytics szolgáltatás a kihagyott edzéseket és a javaslatot számolja.
/// </summary>
public class PlanService(TrainingDbContext db, IPublishEndpoint publisher, TimeProvider time) : IPlanService
{
    /// <summary>
    /// Egy futó tervei, a legutóbb kezdődő elöl.
    /// </summary>
    /// <returns>A tervek, vagy null, ha a felhasználó nem kezelheti a futó adatait.</returns>
    public async Task<List<PlanResponse>?> ListAsync(ClaimsPrincipal user, Guid? runnerId, CancellationToken ct)
    {
        var targetRunnerId = runnerId ?? user.GetUserId();

        // A futó a sajátját, az edző a hozzárendelt futóét kezelheti (spec 3.1).
        if (!await db.CanViewRunnerAsync(user, targetRunnerId, ct))
        {
            return null;
        }

        var plans = await db.TrainingPlans
            .Include(plan => plan.Workouts)
            .Where(plan => plan.RunnerId == targetRunnerId)
            .OrderByDescending(plan => plan.StartDate)
            .ToListAsync(ct);

        return plans.Select(PlanResponse.From).ToList();
    }

    /// <summary>
    /// Egy terv a tervezett edzéseivel.
    /// </summary>
    /// <returns>A terv, vagy null, ha nem létezik vagy a felhasználó nem kezelheti.</returns>
    public async Task<PlanResponse?> GetAsync(ClaimsPrincipal user, Guid id, CancellationToken ct)
    {
        var plan = await FindAccessiblePlanAsync(user, id, ct);
        return plan is null ? null : PlanResponse.From(plan);
    }

    /// <summary>
    /// Új terv a megadott futónak (futónál alapértelmezetten saját magának).
    /// </summary>
    /// <returns>Az új terv, vagy null, ha a felhasználó nem kezelheti a futó adatait.</returns>
    public async Task<PlanResponse?> CreateAsync(ClaimsPrincipal user, Guid? runnerId, ValidPlanInput input, CancellationToken ct)
    {
        var targetRunnerId = runnerId ?? user.GetUserId();
        if (!await db.CanViewRunnerAsync(user, targetRunnerId, ct))
        {
            return null;
        }

        var goalId = input.LinkActiveGoal ? await FindActiveGoalIdAsync(targetRunnerId, ct) : null;
        var plan = TrainingPlan.Create(targetRunnerId, user.GetUserId(), input, goalId, time.GetUtcNow());
        db.TrainingPlans.Add(plan);
        await db.SaveChangesAsync(ct);

        return PlanResponse.From(plan);
    }

    /// <summary>
    /// A terv adatainak (név, időszak, célhoz kötés) módosítása.
    /// </summary>
    /// <returns>A módosított terv, vagy null, ha nem létezik vagy a felhasználó nem kezelheti.</returns>
    public async Task<PlanResponse?> UpdateAsync(ClaimsPrincipal user, Guid id, ValidPlanInput input, CancellationToken ct)
    {
        var plan = await FindAccessiblePlanAsync(user, id, ct);
        if (plan is null)
        {
            return null;
        }

        var goalId = input.LinkActiveGoal ? await FindActiveGoalIdAsync(plan.RunnerId, ct) : null;
        plan.Update(input, goalId);
        await db.SaveChangesAsync(ct);

        return PlanResponse.From(plan);
    }

    /// <summary>
    /// A terv törlése a tervezett edzéseivel együtt; minden törölt tervezett edzésről esemény megy ki.
    /// </summary>
    /// <returns>true, ha törölte; false, ha nem létezik vagy a felhasználó nem kezelheti.</returns>
    public async Task<bool> DeleteAsync(ClaimsPrincipal user, Guid id, CancellationToken ct)
    {
        var plan = await FindAccessiblePlanAsync(user, id, ct);
        if (plan is null)
        {
            return false;
        }

        var now = time.GetUtcNow();
        foreach (var plannedWorkout in plan.Workouts)
        {
            await publisher.Publish(new PlannedWorkoutRemoved(plannedWorkout.Id, plannedWorkout.RunnerId, now), ct);
        }

        // A tervezett edzések kaszkádolva törlődnek a tervvel.
        db.TrainingPlans.Remove(plan);
        await db.SaveChangesAsync(ct);

        return true;
    }

    /// <summary>
    /// Egy futó tervezett edzései egy időszakban, az összes tervéből, dátum szerint rendezve (naptár).
    /// </summary>
    /// <returns>A tervezett edzések, vagy null, ha a felhasználó nem láthatja a futó adatait.</returns>
    public async Task<List<PlannedWorkoutResponse>?> ListPlannedWorkoutsAsync(
        ClaimsPrincipal user, Guid? runnerId, DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        var targetRunnerId = runnerId ?? user.GetUserId();
        if (!await db.CanViewRunnerAsync(user, targetRunnerId, ct))
        {
            return null;
        }

        // A szűrők egymásra épülnek; a dátumtartomány mindkét vége inkluzív és elhagyható.
        var query = db.PlannedWorkouts.Where(plannedWorkout => plannedWorkout.RunnerId == targetRunnerId);

        if (from is not null)
        {
            query = query.Where(plannedWorkout => plannedWorkout.Date >= from);
        }

        if (to is not null)
        {
            query = query.Where(plannedWorkout => plannedWorkout.Date <= to);
        }

        var plannedWorkouts = await query.OrderBy(plannedWorkout => plannedWorkout.Date).ToListAsync(ct);
        return plannedWorkouts.Select(PlannedWorkoutResponse.From).ToList();
    }

    /// <summary>
    /// Új tervezett edzés a tervbe, és PlannedWorkoutChanged esemény küldése.
    /// </summary>
    /// <returns>Az új tervezett edzés, vagy null, ha a terv nem létezik vagy a felhasználó nem kezelheti.</returns>
    public async Task<PlannedWorkoutResponse?> AddPlannedWorkoutAsync(
        ClaimsPrincipal user, Guid planId, ValidPlannedWorkoutInput input, CancellationToken ct)
    {
        var plan = await FindAccessiblePlanAsync(user, planId, ct);
        if (plan is null)
        {
            return null;
        }

        var plannedWorkout = PlannedWorkout.Create(plan, input);
        db.PlannedWorkouts.Add(plannedWorkout);

        // A Publish a SaveChanges előtt kell: az outbox így ugyanabban a tranzakcióban menti az eseményt.
        await publisher.Publish(new PlannedWorkoutChanged(plannedWorkout.ToSnapshot(), time.GetUtcNow()), ct);
        await db.SaveChangesAsync(ct);

        return PlannedWorkoutResponse.From(plannedWorkout);
    }

    /// <summary>
    /// Tervezett edzés módosítása vagy áthelyezése másik napra, és PlannedWorkoutChanged esemény küldése.
    /// </summary>
    /// <returns>A módosított tervezett edzés, vagy null, ha nem létezik vagy a felhasználó nem kezelheti.</returns>
    public async Task<PlannedWorkoutResponse?> UpdatePlannedWorkoutAsync(
        ClaimsPrincipal user, Guid planId, Guid id, ValidPlannedWorkoutInput input, CancellationToken ct)
    {
        var plan = await FindAccessiblePlanAsync(user, planId, ct);
        var plannedWorkout = plan?.Workouts.FirstOrDefault(workout => workout.Id == id);
        if (plannedWorkout is null)
        {
            return null;
        }

        plannedWorkout.Update(input, time.GetLocalToday());
        await publisher.Publish(new PlannedWorkoutChanged(plannedWorkout.ToSnapshot(), time.GetUtcNow()), ct);
        await db.SaveChangesAsync(ct);

        return PlannedWorkoutResponse.From(plannedWorkout);
    }

    /// <summary>
    /// Tervezett edzés törlése, és PlannedWorkoutRemoved esemény küldése.
    /// </summary>
    /// <returns>true, ha törölte; false, ha nem létezik vagy a felhasználó nem kezelheti.</returns>
    public async Task<bool> DeletePlannedWorkoutAsync(ClaimsPrincipal user, Guid planId, Guid id, CancellationToken ct)
    {
        var plan = await FindAccessiblePlanAsync(user, planId, ct);
        var plannedWorkout = plan?.Workouts.FirstOrDefault(workout => workout.Id == id);
        if (plannedWorkout is null)
        {
            return false;
        }

        db.PlannedWorkouts.Remove(plannedWorkout);
        await publisher.Publish(new PlannedWorkoutRemoved(plannedWorkout.Id, plannedWorkout.RunnerId, time.GetUtcNow()), ct);
        await db.SaveChangesAsync(ct);

        return true;
    }

    /// <summary>
    /// A mai nap előtti, még tervezett állapotú edzések kihagyottra váltanak, mindegyikről esemény megy ki.
    /// </summary>
    /// <returns>Az újonnan kihagyottra állított edzések száma.</returns>
    public async Task<int> MarkMissedAsync(DateOnly today, CancellationToken ct)
    {
        // Lejárt az, aminek a napja a mai nap előtt volt, és még nem teljesült.
        var overdue = await db.PlannedWorkouts
            .Where(plannedWorkout => plannedWorkout.Status == PlannedWorkoutStatus.Planned && plannedWorkout.Date < today)
            .ToListAsync(ct);

        var now = time.GetUtcNow();
        foreach (var plannedWorkout in overdue)
        {
            plannedWorkout.MarkMissed();
            await publisher.Publish(new PlannedWorkoutChanged(plannedWorkout.ToSnapshot(), now), ct);
        }

        await db.SaveChangesAsync(ct);
        return overdue.Count;
    }

    /// <summary>
    /// Betölti a tervet a tervezett edzéseivel, ha a felhasználó kezelheti a terv futóját.
    /// </summary>
    /// <returns>A terv, vagy null, ha nem létezik vagy a felhasználó nem kezelheti.</returns>
    private async Task<TrainingPlan?> FindAccessiblePlanAsync(ClaimsPrincipal user, Guid id, CancellationToken ct)
    {
        var plan = await db.TrainingPlans.Include(plan => plan.Workouts).FirstOrDefaultAsync(plan => plan.Id == id, ct);

        // Idegen tervnél is null (404), hogy a létezése se derüljön ki.
        if (plan is null || !await db.CanViewRunnerAsync(user, plan.RunnerId, ct))
        {
            return null;
        }

        return plan;
    }

    /// <summary>
    /// A futó aktív céljának azonosítója, ha van.
    /// </summary>
    private async Task<Guid?> FindActiveGoalIdAsync(Guid runnerId, CancellationToken ct)
    {
        return await db.Goals
            .Where(goal => goal.RunnerId == runnerId && goal.IsActive)
            .Select(goal => (Guid?)goal.Id)
            .FirstOrDefaultAsync(ct);
    }
}
