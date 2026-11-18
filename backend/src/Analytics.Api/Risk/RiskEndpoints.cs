using System.Security.Claims;
using BuildingBlocks.Auth;

namespace Analytics.Api.Risk;

/// <summary>
/// A kockázati pontszám HTTP végpontjai.
/// </summary>
public static class RiskEndpoints
{
    /// <summary>
    /// Regisztrálja a végpontokat a /api/analytics útvonalon.
    /// </summary>
    public static void MapRiskEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/analytics/risk", GetAsync).RequireAuthorization(Policies.RunnerOrCoach);
    }

    /// <summary>
    /// GET /api/analytics/risk?runnerId= – aktuális kockázati pontszám, indoklások és az alakulása (UC15).
    /// </summary>
    private static async Task<IResult> GetAsync(Guid? runnerId, ClaimsPrincipal user, IRiskService service, CancellationToken ct)
    {
        var risk = await service.GetAsync(user, runnerId, ct);
        return risk is null ? Results.Forbid() : Results.Ok(risk);
    }
}
