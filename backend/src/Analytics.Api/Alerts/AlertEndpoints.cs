using System.Security.Claims;
using BuildingBlocks.Auth;

namespace Analytics.Api.Alerts;

/// <summary>
/// A figyelmeztetések HTTP végpontjai.
/// </summary>
public static class AlertEndpoints
{
    /// <summary>
    /// Regisztrálja a végpontokat a /api/analytics/alerts útvonalon.
    /// </summary>
    public static void MapAlertEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/analytics/alerts").RequireAuthorization(Policies.RunnerOrCoach);

        group.MapGet("/", ListAsync).Produces<List<AlertResponse>>();
        group.MapPost("/{id:guid}/acknowledge", AcknowledgeAsync).Produces<AlertResponse>();
    }

    /// <summary>
    /// GET /api/analytics/alerts?runnerId=&amp;includeAcknowledged= – figyelmeztetések súlyosság szerint.
    /// </summary>
    private static async Task<IResult> ListAsync(
        Guid? runnerId, bool? includeAcknowledged, ClaimsPrincipal user, IAlertService service, CancellationToken ct)
    {
        var alerts = await service.ListAsync(user, runnerId, includeAcknowledged ?? false, ct);
        return alerts is null ? Results.Forbid() : Results.Ok(alerts);
    }

    /// <summary>
    /// POST /api/analytics/alerts/{id}/acknowledge – figyelmeztetés nyugtázása.
    /// </summary>
    private static async Task<IResult> AcknowledgeAsync(Guid id, ClaimsPrincipal user, IAlertService service, CancellationToken ct)
    {
        var alert = await service.AcknowledgeAsync(user, id, ct);
        return alert is null ? Results.NotFound() : Results.Ok(alert);
    }
}
