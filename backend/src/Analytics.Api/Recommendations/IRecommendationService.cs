using System.Security.Claims;

namespace Analytics.Api.Recommendations;

/// <summary>
/// Az ajánlott következő edzés (UC10).
/// </summary>
public interface IRecommendationService
{
    /// <summary>
    /// A javaslat a mai napra. Ha nincs megadva futó, a bejelentkezett felhasználónak.
    /// </summary>
    /// <returns>A javaslat, vagy null, ha a felhasználó nem láthatja a futó adatait.</returns>
    Task<Recommendation?> GetAsync(ClaimsPrincipal user, Guid? runnerId, CancellationToken ct);
}
