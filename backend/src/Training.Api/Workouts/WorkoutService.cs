using System.Security.Claims;
using BuildingBlocks.Auth;
using BuildingBlocks.CoachAccess;
using Contracts.Training;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Training.Api.Persistence;

namespace Training.Api.Workouts;

/// <summary>
/// Az edzések üzleti logikája. Minden változás után eseményt küld,
/// amiből az Analytics szolgáltatás újraszámolja a terhelést.
/// </summary>
public class WorkoutService(TrainingDbContext db, IPublishEndpoint publisher, TimeProvider time) : IWorkoutService
{
    /// <summary>
    /// Egy futó edzései dátum szerint csökkenő sorrendben, opcionális dátumtartománnyal.
    /// </summary>
    /// <returns>Az edzések, vagy null, ha a felhasználó nem láthatja a futó adatait.</returns>
    public async Task<List<WorkoutResponse>?> ListAsync(
        ClaimsPrincipal user, Guid? runnerId, DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        var targetRunnerId = runnerId ?? user.GetUserId();

        if (!await db.CanViewRunnerAsync(user, targetRunnerId, ct))
        {
            return null;
        }

        var query = db.Workouts.Where(workout => workout.RunnerId == targetRunnerId);

        if (from is not null)
        {
            query = query.Where(workout => workout.Date >= from);
        }

        if (to is not null)
        {
            query = query.Where(workout => workout.Date <= to);
        }

        var workouts = await query.OrderByDescending(workout => workout.Date).ToListAsync(ct);
        return workouts.Select(WorkoutResponse.From).ToList();
    }

    /// <summary>
    /// Egy edzés azonosító alapján, ha a felhasználó láthatja.
    /// </summary>
    /// <returns>Az edzés, vagy null, ha nem létezik vagy a felhasználó nem láthatja.</returns>
    public async Task<WorkoutResponse?> GetAsync(ClaimsPrincipal user, Guid id, CancellationToken ct)
    {
        var workout = await db.Workouts.FirstOrDefaultAsync(workout => workout.Id == id, ct);

        // Idegen edzésnél is null-t adunk (404), hogy a létezése se derüljön ki.
        if (workout is null || !await db.CanViewRunnerAsync(user, workout.RunnerId, ct))
        {
            return null;
        }

        return WorkoutResponse.From(workout);
    }

    /// <summary>
    /// Új edzés rögzítése a bejelentkezett futó nevére, és WorkoutRecorded esemény küldése.
    /// Ha tervezett edzéshez tartozik (UC08), az teljesítettre vált.
    /// </summary>
    /// <returns>A mentett edzés, vagy null, ha a megadott tervezett edzés nem a futóé vagy már teljesült.</returns>
    public async Task<WorkoutResponse?> CreateAsync(ClaimsPrincipal user, ValidWorkoutInput input, CancellationToken ct)
    {
        var runnerId = user.GetUserId();
        var now = time.GetUtcNow();
        var workout = Workout.Create(runnerId, input, now);
        db.Workouts.Add(workout);

        if (input.PlannedWorkoutId is { } plannedWorkoutId)
        {
            // Csak a futó saját, még nem teljesült tervezett edzése kapcsolható (a kihagyott is pótolható).
            var plannedWorkout = await db.PlannedWorkouts.FirstOrDefaultAsync(planned =>
                planned.Id == plannedWorkoutId
                && planned.RunnerId == runnerId
                && planned.Status != PlannedWorkoutStatus.Completed, ct);

            if (plannedWorkout is null)
            {
                return null;
            }

            plannedWorkout.Complete(workout.Id);
            await publisher.Publish(new PlannedWorkoutChanged(plannedWorkout.ToSnapshot(), now), ct);
        }

        await publisher.Publish(new WorkoutRecorded(workout.ToSnapshot(), now), ct);
        await db.SaveChangesAsync(ct);

        return WorkoutResponse.From(workout);
    }

    /// <summary>
    /// A futó saját edzésének módosítása, és WorkoutUpdated esemény küldése.
    /// </summary>
    /// <returns>A módosított edzés, vagy null, ha nincs ilyen saját edzés.</returns>
    public async Task<WorkoutResponse?> UpdateAsync(
        ClaimsPrincipal user, Guid id, ValidWorkoutInput input, CancellationToken ct)
    {
        var userId = user.GetUserId();
        var workout = await db.Workouts.FirstOrDefaultAsync(workout => workout.Id == id && workout.RunnerId == userId, ct);

        if (workout is null)
        {
            return null;
        }

        var now = time.GetUtcNow();
        workout.Update(input, now);
        await publisher.Publish(new WorkoutUpdated(workout.ToSnapshot(), now), ct);
        await db.SaveChangesAsync(ct);

        return WorkoutResponse.From(workout);
    }

    /// <summary>
    /// A futó saját edzésének törlése, és WorkoutDeleted esemény küldése. Ha tervezett edzést
    /// teljesített, az újra tervezett lesz.
    /// </summary>
    /// <returns>true, ha törölte; false, ha nincs ilyen saját edzés.</returns>
    public async Task<bool> DeleteAsync(ClaimsPrincipal user, Guid id, CancellationToken ct)
    {
        var userId = user.GetUserId();
        var workout = await db.Workouts.FirstOrDefaultAsync(workout => workout.Id == id && workout.RunnerId == userId, ct);
        if (workout is null)
        {
            return false;
        }

        var now = time.GetUtcNow();
        if (workout.PlannedWorkoutId is { } plannedWorkoutId)
        {
            // A terv azóta törölve lehet, ezért a tervezett edzés hiánya nem hiba.
            var plannedWorkout = await db.PlannedWorkouts.FirstOrDefaultAsync(planned => planned.Id == plannedWorkoutId, ct);
            if (plannedWorkout is not null)
            {
                plannedWorkout.Reopen();
                await publisher.Publish(new PlannedWorkoutChanged(plannedWorkout.ToSnapshot(), now), ct);
            }
        }

        db.Workouts.Remove(workout);
        await publisher.Publish(new WorkoutDeleted(workout.Id, workout.RunnerId, workout.Date, now), ct);
        await db.SaveChangesAsync(ct);

        return true;
    }
}