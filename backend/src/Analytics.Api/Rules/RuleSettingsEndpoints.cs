using BuildingBlocks.Auth;

namespace Analytics.Api.Rules;

/// <summary>
/// A szabálybeállítások HTTP végpontjai; csak az adminisztrátor éri el őket.
/// </summary>
public static class RuleSettingsEndpoints
{
    /// <summary>
    /// Regisztrálja a végpontokat a /api/analytics/admin/rules útvonalon.
    /// </summary>
    public static void MapRuleSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/analytics/admin/rules").RequireAuthorization(Policies.Admin);

        group.MapGet("/", GetAsync);
        group.MapPut("/", UpdateAsync);
    }

    /// <summary>
    /// GET /api/analytics/admin/rules – a küszöbök és a szabályok aktív állapota.
    /// </summary>
    private static async Task<IResult> GetAsync(IRuleSettingsService service, CancellationToken ct)
    {
        return Results.Ok(await service.GetAsync(ct));
    }

    /// <summary>
    /// PUT /api/analytics/admin/rules – a beállítások felülírása (UC17).
    /// </summary>
    private static async Task<IResult> UpdateAsync(RuleSettingsDto request, IRuleSettingsService service, CancellationToken ct)
    {
        if (!RuleSettingsValidator.TryValidate(request, out var errors))
        {
            return Results.ValidationProblem(errors);
        }

        return Results.Ok(await service.UpdateAsync(request, ct));
    }
}
