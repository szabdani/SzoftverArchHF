using System.Security.Claims;
using Analytics.Api.Persistence;
using BuildingBlocks.Auth;
using BuildingBlocks.CoachAccess;
using Microsoft.EntityFrameworkCore;

namespace Analytics.Api.Alerts;

/// <summary>
/// A figyelmeztetések listázása és nyugtázása. A figyelmeztetéseket a <see cref="Risk.RiskService"/> hozza létre
/// minden újraszámoláskor.
/// </summary>
public class AlertService(AnalyticsDbContext db, TimeProvider time) : IAlertService
{
    /// <summary>
    /// Egy futó figyelmeztetései súlyosság, azon belül időpont szerint csökkenően.
    /// </summary>
    /// <returns>A figyelmeztetések, vagy null, ha a felhasználó nem láthatja a futó adatait.</returns>
    public async Task<List<AlertResponse>?> ListAsync(ClaimsPrincipal user, Guid? runnerId, bool includeAcknowledged, CancellationToken ct)
    {
        var targetRunnerId = runnerId ?? user.GetUserId();

        // A figyelmeztetés a futónak és a hozzárendelt edzőnek jelenik meg (spec 3.6), az adminnak nem.
        if (!await db.CanViewRunnerAsync(user, targetRunnerId, ct))
        {
            return null;
        }

        var query = db.Alerts.Where(alert => alert.RunnerId == targetRunnerId);

        // Alapból csak a még nyugtázatlanok.
        if (!includeAcknowledged)
        {
            query = query.Where(alert => alert.AcknowledgedAt == null);
        }

        var alerts = await query.ToListAsync(ct);

        // A súlyosság szövegként van tárolva, ezért a rendezés memóriában, az enum sorrendje szerint történik.
        return alerts
            .OrderByDescending(alert => alert.Severity)
            .ThenByDescending(alert => alert.CreatedAt)
            .Select(AlertResponse.From)
            .ToList();
    }

    /// <summary>
    /// Egy figyelmeztetés nyugtázása (ismételt nyugtázásnál az első időpont marad).
    /// </summary>
    /// <returns>A nyugtázott figyelmeztetés, vagy null, ha nem létezik vagy a felhasználó nem kezelheti.</returns>
    public async Task<AlertResponse?> AcknowledgeAsync(ClaimsPrincipal user, Guid id, CancellationToken ct)
    {
        var alert = await db.Alerts.FirstOrDefaultAsync(alert => alert.Id == id, ct);

        // Idegen figyelmeztetésnél is null (404), hogy a létezése se derüljön ki.
        if (alert is null || !await db.CanViewRunnerAsync(user, alert.RunnerId, ct))
        {
            return null;
        }

        alert.AcknowledgedAt ??= time.GetUtcNow();
        await db.SaveChangesAsync(ct);

        return AlertResponse.From(alert);
    }
}
