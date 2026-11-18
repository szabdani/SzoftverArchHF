using System.Security.Claims;

namespace Analytics.Api.Risk;

/// <summary>
/// A kockázati pontszám számítása és lekérdezése (UC15, UC20).
/// </summary>
public interface IRiskService
{
    /// <summary>
    /// Egy futó aktuális pontszáma és az elmúlt 12 hét alakulása. Ha nincs megadva futó, a bejelentkezett felhasználóé.
    /// </summary>
    /// <returns>A kockázati nézet, vagy null, ha a felhasználó nem láthatja a futó adatait.</returns>
    Task<RiskResponse?> GetAsync(ClaimsPrincipal user, Guid? runnerId, CancellationToken ct);

    /// <summary>
    /// Újraszámolja és eltárolja a futó mai pontszámát (minden edzés-esemény után, UC20).
    /// </summary>
    Task RecalculateAsync(Guid runnerId, CancellationToken ct);

    /// <summary>
    /// Minden futónak újraszámolja a mai pontszámát (pl. a küszöbök módosítása után).
    /// </summary>
    /// <returns>Az újraszámolt futók száma.</returns>
    Task<int> RecalculateAllAsync(CancellationToken ct);

    /// <summary>
    /// A napi ellenőrzés része (UC19): minden futónak kiszámolja a mai pontszámát, akinek még nincs.
    /// </summary>
    /// <returns>Az újraszámolt futók száma.</returns>
    Task<int> RecalculateMissingAsync(CancellationToken ct);
}
