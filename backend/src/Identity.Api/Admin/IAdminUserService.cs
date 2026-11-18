namespace Identity.Api.Admin;

/// <summary>
/// Felhasználók és szerepkörök kezelése az adminisztrátornak (UC16), a Keycloakon keresztül.
/// </summary>
public interface IAdminUserService
{
    /// <summary>
    /// Felhasználók keresése (név, e-mail részlet) a szerepkörükkel; a technikai service account nélkül.
    /// </summary>
    Task<List<AdminUserResponse>> SearchAsync(string? search, CancellationToken ct);

    /// <summary>
    /// A felhasználó szerepkörének beállítása (runner, coach vagy admin).
    /// </summary>
    /// <returns>false, ha a felhasználó nem létezik.</returns>
    Task<bool> SetRoleAsync(Guid userId, string role, CancellationToken ct);

    /// <summary>
    /// A felhasználó engedélyezése vagy letiltása.
    /// </summary>
    /// <returns>false, ha a felhasználó nem létezik.</returns>
    Task<bool> SetEnabledAsync(Guid userId, bool enabled, CancellationToken ct);
}
