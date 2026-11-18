namespace BuildingBlocks.Auth;

public sealed class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    /// <summary>A tokenben szereplő kibocsátó (a böngésző felől látott realm URL).</summary>
    public required string Authority { get; init; }

    /// <summary>
    /// Opcionális belső metaadat-cím. Docker hálózaton belül a szolgáltatás a Keycloakot
    /// a belső hosztnéven éri el, miközben a token kibocsátója a publikus URL.
    /// </summary>
    public string? MetadataAddress { get; init; }

    public string Audience { get; init; } = "futoedzes-api";

    public bool RequireHttpsMetadata { get; init; }
}
