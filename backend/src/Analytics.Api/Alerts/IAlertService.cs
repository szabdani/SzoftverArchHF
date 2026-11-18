using System.Security.Claims;

namespace Analytics.Api.Alerts;

/// <summary>
/// Figyelmeztetések listázása és nyugtázása (UC09). A futó a sajátját, a hozzárendelt edző a futóét kezeli.
/// </summary>
public interface IAlertService
{
    /// <summary>
    /// Egy futó figyelmeztetései súlyosság, azon belül időpont szerint csökkenően.
    /// </summary>
    /// <param name="includeAcknowledged">A már nyugtázottak is kellenek-e.</param>
    /// <returns>A figyelmeztetések, vagy null, ha a felhasználó nem láthatja a futó adatait.</returns>
    Task<List<AlertResponse>?> ListAsync(ClaimsPrincipal user, Guid? runnerId, bool includeAcknowledged, CancellationToken ct);

    /// <summary>
    /// Egy figyelmeztetés nyugtázása (ismételt nyugtázásnál az első időpont marad).
    /// </summary>
    /// <returns>A nyugtázott figyelmeztetés, vagy null, ha nem létezik vagy a felhasználó nem kezelheti.</returns>
    Task<AlertResponse?> AcknowledgeAsync(ClaimsPrincipal user, Guid id, CancellationToken ct);
}
