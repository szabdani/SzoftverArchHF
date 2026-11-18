using System.Security.Claims;

namespace Training.Api.Goals;

/// <summary>
/// A célok kezelése (UC05): a futó kitűzi és módosítja, az edző megtekinti.
/// </summary>
public interface IGoalService
{
    /// <summary>
    /// Egy futó összes célja, az aktív elöl. Ha nincs megadva futó, a bejelentkezett felhasználóé.
    /// </summary>
    /// <returns>A célok, vagy null, ha a felhasználó nem láthatja a futó adatait.</returns>
    Task<List<GoalResponse>?> ListAsync(ClaimsPrincipal user, Guid? runnerId, CancellationToken ct);

    /// <summary>
    /// A célnézet: az aktív cél, a hátralévő hetek és a legutóbbi edzések.
    /// </summary>
    /// <returns>A célnézet, vagy null, ha nincs aktív cél, vagy a felhasználó nem láthatja a futót.</returns>
    Task<ActiveGoalResponse?> GetActiveAsync(ClaimsPrincipal user, Guid? runnerId, CancellationToken ct);

    /// <summary>
    /// Új aktív cél kitűzése a bejelentkezett futónak; az előző aktív cél archiválódik.
    /// </summary>
    Task<GoalResponse> CreateAsync(ClaimsPrincipal user, ValidGoalInput input, CancellationToken ct);

    /// <summary>
    /// A futó saját aktív céljának módosítása.
    /// </summary>
    /// <returns>A módosított cél, vagy null, ha nincs ilyen saját aktív cél.</returns>
    Task<GoalResponse?> UpdateAsync(ClaimsPrincipal user, Guid id, ValidGoalInput input, CancellationToken ct);
}
