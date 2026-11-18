using System.Security.Claims;
using Analytics.Api.Persistence;
using BuildingBlocks.Auth;
using BuildingBlocks.CoachAccess;
using BuildingBlocks.Time;
using Contracts.Training;
using Microsoft.EntityFrameworkCore;

namespace Analytics.Api.Risk;

/// <summary>
/// A kockázati pontszám tárolása és lekérdezése. Maga a számítás a <see cref="RiskCalculator"/>-ban van.
/// </summary>
public class RiskService(AnalyticsDbContext db, TimeProvider time) : IRiskService
{
    private const int HistoryDays = 84;

    /// <summary>
    /// Egy futó aktuális pontszáma és az elmúlt 12 hét alakulása, dátum szerint növekvően.
    /// </summary>
    /// <returns>A kockázati nézet, vagy null, ha a felhasználó nem láthatja a futó adatait.</returns>
    public async Task<RiskResponse?> GetAsync(ClaimsPrincipal user, Guid? runnerId, CancellationToken ct)
    {
        var targetRunnerId = runnerId ?? user.GetUserId();

        // Szerveroldali jogosultság-ellenőrzés: az edző csak hozzárendelt futót láthat, az admin senkit.
        if (!await db.CanViewRunnerAsync(user, targetRunnerId, ct))
        {
            return null;
        }

        var from = time.GetLocalToday().AddDays(-HistoryDays + 1);
        var snapshots = await db.RiskScoreSnapshots
            .Where(snapshot => snapshot.RunnerId == targetRunnerId && snapshot.Date >= from)
            .OrderBy(snapshot => snapshot.Date)
            .ToListAsync(ct);

        var history = snapshots.Select(RiskScoreResponse.From).ToList();
        return new RiskResponse(history.LastOrDefault(), history);
    }

    /// <summary>
    /// Újraszámolja és eltárolja (felülírja) a futó mai pontszámát.
    /// </summary>
    public async Task RecalculateAsync(Guid runnerId, CancellationToken ct)
    {
        var today = time.GetLocalToday();
        var windowStart = today.AddDays(-27);

        // A számításhoz csak az utolsó 28 nap edzései kellenek.
        var workouts = await db.Workouts
            .Where(workout => workout.RunnerId == runnerId && workout.Date >= windowStart && workout.Date <= today)
            .ToListAsync(ct);

        // A legelső edzés napjából derül ki, van-e már 28 napnyi adat az ACWR-hez.
        var firstWorkoutDate = await db.Workouts
            .Where(workout => workout.RunnerId == runnerId)
            .MinAsync(workout => (DateOnly?)workout.Date, ct);

        // Kihagyott tervezett edzések az utolsó 7 napban.
        var missedLast7Days = await db.PlannedWorkouts.CountAsync(plannedWorkout =>
            plannedWorkout.RunnerId == runnerId
            && plannedWorkout.Status == PlannedWorkoutStatus.Missed
            && plannedWorkout.Date >= today.AddDays(-6)
            && plannedWorkout.Date <= today, ct);

        var result = RiskCalculator.Calculate(workouts, missedLast7Days, today, firstWorkoutDate);

        var snapshot = await db.RiskScoreSnapshots.FirstOrDefaultAsync(snapshot => snapshot.RunnerId == runnerId && snapshot.Date == today, ct);
        if (snapshot is null)
        {
            snapshot = new RiskScoreSnapshot { Id = Guid.CreateVersion7(), RunnerId = runnerId, Date = today };
            db.RiskScoreSnapshots.Add(snapshot);
        }

        snapshot.Apply(result, time.GetUtcNow());
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Minden futónak kiszámolja a mai pontszámát, akiről van adat, de mára még nincs pontszáma.
    /// </summary>
    /// <returns>Az újraszámolt futók száma.</returns>
    public async Task<int> RecalculateMissingAsync(CancellationToken ct)
    {
        var today = time.GetLocalToday();

        // Akiről van edzés vagy tervezett edzés, kivéve akiknek a mai pontszáma már megvan.
        var runnerIds = await db.Workouts.Select(workout => workout.RunnerId)
            .Union(db.PlannedWorkouts.Select(plannedWorkout => plannedWorkout.RunnerId))
            .Except(db.RiskScoreSnapshots.Where(snapshot => snapshot.Date == today).Select(snapshot => snapshot.RunnerId))
            .ToListAsync(ct);

        foreach (var runnerId in runnerIds)
        {
            await RecalculateAsync(runnerId, ct);
        }

        return runnerIds.Count;
    }
}
