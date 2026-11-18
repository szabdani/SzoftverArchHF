using System.Security.Claims;
using BuildingBlocks.Auth;

namespace Identity.Api.Admin;

/// <summary>
/// A felhasználókezelés HTTP végpontjai; csak adminisztrátor éri el őket (adminisztrátort is csak admin nevezhet ki).
/// </summary>
public static class AdminUserEndpoints
{
    /// <summary>
    /// Regisztrálja a végpontokat a /api/identity/admin/users útvonalon.
    /// </summary>
    public static void MapAdminUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/identity/admin/users").RequireAuthorization(Policies.Admin);

        group.MapGet("/", SearchAsync);
        group.MapPut("/{id:guid}/role", SetRoleAsync);
        group.MapPut("/{id:guid}/enabled", SetEnabledAsync);
    }

    /// <summary>
    /// GET /api/identity/admin/users?search= – felhasználók keresése szerepkörrel.
    /// </summary>
    private static async Task<IResult> SearchAsync(string? search, IAdminUserService service, CancellationToken ct)
    {
        return Results.Ok(await service.SearchAsync(search, ct));
    }

    /// <summary>
    /// PUT /api/identity/admin/users/{id}/role – szerepkör módosítása (saját magáé nem).
    /// </summary>
    private static async Task<IResult> SetRoleAsync(
        Guid id, SetRoleRequest request, ClaimsPrincipal user, IAdminUserService service, CancellationToken ct)
    {
        if (request.Role is not { } role || !AdminUserService.AppRoles.Contains(role))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["role"] = ["Érvényes értékek: runner, coach, admin."] });
        }

        // Így az utolsó adminisztrátor sem veheti el véletlenül a saját jogát.
        if (id == user.GetUserId())
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["id"] = ["A saját szerepkörödet nem módosíthatod."] });
        }

        return await service.SetRoleAsync(id, role, ct) ? Results.NoContent() : Results.NotFound();
    }

    /// <summary>
    /// PUT /api/identity/admin/users/{id}/enabled – felhasználó letiltása vagy engedélyezése (saját magáé nem).
    /// </summary>
    private static async Task<IResult> SetEnabledAsync(
        Guid id, SetEnabledRequest request, ClaimsPrincipal user, IAdminUserService service, CancellationToken ct)
    {
        if (id == user.GetUserId())
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["id"] = ["Saját magadat nem tilthatod le."] });
        }

        return await service.SetEnabledAsync(id, request.Enabled, ct) ? Results.NoContent() : Results.NotFound();
    }
}
