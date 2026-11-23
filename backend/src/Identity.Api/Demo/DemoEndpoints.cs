using BuildingBlocks.Auth;

namespace Identity.Api.Demo;

/// <summary>
/// A demóadatok HTTP végpontjai; csak adminisztrátor éri el őket (UC18).
/// </summary>
public static class DemoEndpoints
{
    /// <summary>
    /// Regisztrálja a végpontokat a /api/identity/admin/demo útvonalon.
    /// </summary>
    public static void MapDemoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/identity/admin/demo").RequireAuthorization(Policies.Admin);

        group.MapPost("/", CreateAsync).Produces<List<DemoRunnerResponse>>();
        group.MapDelete("/", RemoveAsync).Produces(StatusCodes.Status204NoContent);
    }

    /// <summary>
    /// POST /api/identity/admin/demo – szimulált futók és 12 hét adat létrehozása (újragenerálás).
    /// </summary>
    private static async Task<IResult> CreateAsync(IDemoService service, CancellationToken ct)
    {
        return Results.Ok(await service.CreateAsync(ct));
    }

    /// <summary>
    /// DELETE /api/identity/admin/demo – a demóadatok törlése.
    /// </summary>
    private static async Task<IResult> RemoveAsync(IDemoService service, CancellationToken ct)
    {
        await service.RemoveAsync(ct);
        return Results.NoContent();
    }
}
