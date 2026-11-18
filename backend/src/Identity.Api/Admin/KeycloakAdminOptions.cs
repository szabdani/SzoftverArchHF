namespace Identity.Api.Admin;

/// <summary>
/// A Keycloak Admin API elérése: a realm címe és a felhasználókezeléshez jogosult service account kliens.
/// </summary>
public sealed class KeycloakAdminOptions
{
    public const string SectionName = "KeycloakAdmin";

    /// <summary>A Keycloak alapcíme a /auth előtaggal (Docker hálózaton belül a belső cím).</summary>
    public string BaseUrl { get; init; } = "http://localhost/auth";

    public string Realm { get; init; } = "futoedzes";
    public string ClientId { get; init; } = "futoedzes-identity-admin";
    public string ClientSecret { get; init; } = "";
}
