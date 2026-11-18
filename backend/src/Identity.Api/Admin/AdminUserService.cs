using BuildingBlocks.Auth;

namespace Identity.Api.Admin;

/// <summary>
/// A felhasználókezelés logikája a Keycloak Admin API fölött.
/// </summary>
public class AdminUserService(KeycloakAdminClient keycloak) : IAdminUserService
{
    /// <summary>Az alkalmazás szerepkörei erősség szerint csökkenően.</summary>
    public static readonly string[] AppRoles = [Roles.Admin, Roles.Coach, Roles.Runner];

    private const int MaxResults = 50;

    /// <summary>
    /// Felhasználók keresése a szerepkörükkel; a technikai service account nélkül.
    /// </summary>
    public async Task<List<AdminUserResponse>> SearchAsync(string? search, CancellationToken ct)
    {
        var users = await keycloak.SearchUsersAsync(search, MaxResults, ct);

        var result = new List<AdminUserResponse>();
        foreach (var user in users.Where(user => !user.Username.StartsWith("service-account-", StringComparison.Ordinal)))
        {
            // Felhasználónként egy hívás a tényleges szerepkörökért; a keresés legfeljebb 50 találatot ad.
            var roles = await keycloak.GetEffectiveRealmRolesAsync(user.Id, ct);
            var role = AppRoles.FirstOrDefault(roles.Contains);
            var name = $"{user.FirstName} {user.LastName}".Trim();
            result.Add(new AdminUserResponse(Guid.Parse(user.Id), user.Email, name, role, user.Enabled));
        }

        return result;
    }

    /// <summary>
    /// A felhasználó szerepkörének beállítása; a többi alkalmazás-szerepkör lekerül róla.
    /// </summary>
    /// <returns>false, ha a felhasználó nem létezik.</returns>
    public Task<bool> SetRoleAsync(Guid userId, string role, CancellationToken ct) =>
        keycloak.SetAppRoleAsync(userId.ToString(), role, AppRoles, ct);

    /// <summary>
    /// A felhasználó engedélyezése vagy letiltása.
    /// </summary>
    /// <returns>false, ha a felhasználó nem létezik.</returns>
    public Task<bool> SetEnabledAsync(Guid userId, bool enabled, CancellationToken ct) =>
        keycloak.SetEnabledAsync(userId.ToString(), enabled, ct);
}
