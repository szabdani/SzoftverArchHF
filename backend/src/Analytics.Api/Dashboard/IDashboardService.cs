using System.Security.Claims;

namespace Analytics.Api.Dashboard;

/// <summary>
/// A terhelési dashboard (UC06) és az edzői áttekintés (UC14).
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// A futó dashboardja a heti vagy havi időszakra. Ha nincs megadva futó, a bejelentkezett felhasználóé.
    /// </summary>
    /// <returns>A dashboard, vagy null, ha a felhasználó nem láthatja a futó adatait.</returns>
    Task<DashboardResponse?> GetAsync(ClaimsPrincipal user, Guid? runnerId, DashboardPeriod period, CancellationToken ct);

    /// <summary>
    /// Az edzőhöz rendelt futók kockázat szerint csökkenő sorrendben.
    /// </summary>
    Task<List<CoachRunnerSummary>> ListCoachRunnersAsync(ClaimsPrincipal coach, CancellationToken ct);
}
