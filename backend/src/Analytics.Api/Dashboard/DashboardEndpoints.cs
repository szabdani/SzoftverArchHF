using System.Security.Claims;
using BuildingBlocks.Auth;

namespace Analytics.Api.Dashboard;

/// <summary>
/// A dashboard és az edzői áttekintés HTTP végpontjai.
/// </summary>
public static class DashboardEndpoints
{
    /// <summary>
    /// Regisztrálja a végpontokat a /api/analytics útvonalon.
    /// </summary>
    public static void MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/analytics/dashboard", GetAsync).RequireAuthorization(Policies.RunnerOrCoach);
        app.MapGet("/api/analytics/coach/runners", ListCoachRunnersAsync).RequireAuthorization(Policies.Coach);
    }

    /// <summary>
    /// GET /api/analytics/dashboard?runnerId=&amp;period=week|month – terhelési dashboard (UC06).
    /// </summary>
    private static async Task<IResult> GetAsync(
        Guid? runnerId, string? period, ClaimsPrincipal user, IDashboardService service, CancellationToken ct)
    {
        DashboardPeriod? parsedPeriod = period?.ToLowerInvariant() switch
        {
            null or "week" => DashboardPeriod.Week,
            "month" => DashboardPeriod.Month,
            _ => null,
        };

        if (parsedPeriod is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["period"] = ["Érvényes értékek: week, month."],
            });
        }

        var dashboard = await service.GetAsync(user, runnerId, parsedPeriod.Value, ct);
        return dashboard is null ? Results.Forbid() : Results.Ok(dashboard);
    }

    /// <summary>
    /// GET /api/analytics/coach/runners – az edző futói kockázat szerint rendezve (UC14).
    /// </summary>
    private static async Task<IResult> ListCoachRunnersAsync(ClaimsPrincipal user, IDashboardService service, CancellationToken ct)
    {
        return Results.Ok(await service.ListCoachRunnersAsync(user, ct));
    }
}
