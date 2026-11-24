using System.Security.Claims;
using BuildingBlocks.Auth;

namespace Analytics.Api.Recommendations;

/// <summary>
/// Az edzésjavaslat HTTP végpontja.
/// </summary>
public static class RecommendationEndpoints
{
    /// <summary>
    /// Regisztrálja a végpontot a /api/analytics/recommendation útvonalon.
    /// </summary>
    public static void MapRecommendationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/analytics/recommendation", GetAsync).RequireAuthorization(Policies.RunnerOrCoach).Produces<Recommendation>();
    }

    /// <summary>
    /// GET /api/analytics/recommendation?runnerId= – a javasolt következő edzés indoklással (UC10).
    /// </summary>
    private static async Task<IResult> GetAsync(Guid? runnerId, ClaimsPrincipal user, IRecommendationService service, CancellationToken ct)
    {
        var recommendation = await service.GetAsync(user, runnerId, ct);
        return recommendation is null ? Results.Forbid() : Results.Ok(recommendation);
    }
}
