using System.Security.Claims;

namespace Training.Api.Plans;

/// <summary>
/// Edzéstervek és tervezett edzések kezelése (UC07, UC11). A tervet a futó a saját magának,
/// az edző a hozzárendelt futójának kezelheti; az adminisztrátor nem.
/// </summary>
public interface IPlanService
{
    /// <summary>
    /// Egy futó tervei, a legutóbb kezdődő elöl. Ha nincs megadva futó, a bejelentkezett felhasználóé.
    /// </summary>
    /// <returns>A tervek, vagy null, ha a felhasználó nem kezelheti a futó adatait.</returns>
    Task<List<PlanResponse>?> ListAsync(ClaimsPrincipal user, Guid? runnerId, CancellationToken ct);

    /// <summary>
    /// Egy terv a tervezett edzéseivel.
    /// </summary>
    /// <returns>A terv, vagy null, ha nem létezik vagy a felhasználó nem kezelheti.</returns>
    Task<PlanResponse?> GetAsync(ClaimsPrincipal user, Guid id, CancellationToken ct);

    /// <summary>
    /// Új terv a megadott futónak (futónál alapértelmezetten saját magának).
    /// </summary>
    /// <returns>Az új terv, vagy null, ha a felhasználó nem kezelheti a futó adatait.</returns>
    Task<PlanResponse?> CreateAsync(ClaimsPrincipal user, Guid? runnerId, ValidPlanInput input, CancellationToken ct);

    /// <summary>
    /// A terv adatainak (név, időszak, célhoz kötés) módosítása.
    /// </summary>
    /// <returns>A módosított terv, vagy null, ha nem létezik vagy a felhasználó nem kezelheti.</returns>
    Task<PlanResponse?> UpdateAsync(ClaimsPrincipal user, Guid id, ValidPlanInput input, CancellationToken ct);

    /// <summary>
    /// A terv törlése a tervezett edzéseivel együtt.
    /// </summary>
    /// <returns>true, ha törölte; false, ha nem létezik vagy a felhasználó nem kezelheti.</returns>
    Task<bool> DeleteAsync(ClaimsPrincipal user, Guid id, CancellationToken ct);

    /// <summary>
    /// Egy futó tervezett edzései egy időszakban, az összes tervéből (naptár, UC07).
    /// </summary>
    /// <returns>A tervezett edzések, vagy null, ha a felhasználó nem láthatja a futó adatait.</returns>
    Task<List<PlannedWorkoutResponse>?> ListPlannedWorkoutsAsync(
        ClaimsPrincipal user, Guid? runnerId, DateOnly? from, DateOnly? to, CancellationToken ct);

    /// <summary>
    /// Új tervezett edzés a tervbe (a bemenet már a terv időszakára ellenőrzött).
    /// </summary>
    /// <returns>Az új tervezett edzés, vagy null, ha a terv nem létezik vagy a felhasználó nem kezelheti.</returns>
    Task<PlannedWorkoutResponse?> AddPlannedWorkoutAsync(
        ClaimsPrincipal user, Guid planId, ValidPlannedWorkoutInput input, CancellationToken ct);

    /// <summary>
    /// Tervezett edzés módosítása vagy áthelyezése másik napra (drag &amp; drop).
    /// </summary>
    /// <returns>A módosított tervezett edzés, vagy null, ha nem létezik vagy a felhasználó nem kezelheti.</returns>
    Task<PlannedWorkoutResponse?> UpdatePlannedWorkoutAsync(
        ClaimsPrincipal user, Guid planId, Guid id, ValidPlannedWorkoutInput input, CancellationToken ct);

    /// <summary>
    /// Tervezett edzés törlése.
    /// </summary>
    /// <returns>true, ha törölte; false, ha nem létezik vagy a felhasználó nem kezelheti.</returns>
    Task<bool> DeletePlannedWorkoutAsync(ClaimsPrincipal user, Guid planId, Guid id, CancellationToken ct);

    /// <summary>
    /// Az ütemezett ellenőrzés része (UC19): a mai nap előtti, még tervezett állapotú edzések kihagyottra váltanak.
    /// </summary>
    /// <returns>Az újonnan kihagyottra állított edzések száma.</returns>
    Task<int> MarkMissedAsync(DateOnly today, CancellationToken ct);
}
