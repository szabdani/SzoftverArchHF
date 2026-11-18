namespace Identity.Api.Admin;

/// <summary>
/// Egy felhasználó az adminisztrátor felé: csak a felhasználókezeléshez szükséges adatok (spec 3.1),
/// edzésadat nélkül. A szerepkör a legmagasabb alkalmazás-szerepkör (admin, coach vagy runner), ha van.
/// </summary>
public record AdminUserResponse(Guid Id, string? Email, string Name, string? Role, bool Enabled);

/// <summary>Szerepkör módosítása: runner, coach vagy admin.</summary>
public record SetRoleRequest(string? Role);

/// <summary>Felhasználó engedélyezése vagy letiltása.</summary>
public record SetEnabledRequest(bool Enabled);
