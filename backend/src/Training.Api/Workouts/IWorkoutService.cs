using System.Security.Claims;

namespace Training.Api.Workouts;

/// <summary>
/// Az edzések kezelése (UC02–UC04).
/// </summary>
public interface IWorkoutService
{
    /// <summary>
    /// Egy futó edzései. Ha nincs megadva futó, a bejelentkezett felhasználóé.
    /// </summary>
    /// <returns>Az edzések, vagy null, ha a felhasználó nem láthatja a futó adatait.</returns>
    Task<List<WorkoutResponse>?> ListAsync(ClaimsPrincipal user, Guid? runnerId, DateOnly? from, DateOnly? to, CancellationToken ct);

    /// <summary>
    /// Egy edzés azonosító alapján.
    /// </summary>
    /// <returns>Az edzés, vagy null, ha nem létezik vagy a felhasználó nem láthatja.</returns>
    Task<WorkoutResponse?> GetAsync(ClaimsPrincipal user, Guid id, CancellationToken ct);

    /// <summary>
    /// Új edzés rögzítése a bejelentkezett futó nevére. Ha tervezett edzéshez tartozik (UC08),
    /// az teljesítettre vált.
    /// </summary>
    /// <returns>A mentett edzés, vagy null, ha a megadott tervezett edzés nem a futóé vagy már teljesült.</returns>
    Task<WorkoutResponse?> CreateAsync(ClaimsPrincipal user, ValidWorkoutInput input, CancellationToken ct);

    /// <summary>
    /// A futó saját edzésének módosítása.
    /// </summary>
    /// <returns>A módosított edzés, vagy null, ha nincs ilyen saját edzés.</returns>
    Task<WorkoutResponse?> UpdateAsync(ClaimsPrincipal user, Guid id, ValidWorkoutInput input, CancellationToken ct);

    /// <summary>
    /// A futó saját edzésének törlése.
    /// </summary>
    /// <returns>true, ha törölte; false, ha nincs ilyen saját edzés.</returns>
    Task<bool> DeleteAsync(ClaimsPrincipal user, Guid id, CancellationToken ct);
}
