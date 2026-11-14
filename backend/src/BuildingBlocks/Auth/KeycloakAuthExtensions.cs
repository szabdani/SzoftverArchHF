using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Auth;

public static class KeycloakAuthExtensions
{
    public const string RoleClaimType = "role";

    public static IServiceCollection AddKeycloakAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var keycloak = configuration.GetSection(KeycloakOptions.SectionName).Get<KeycloakOptions>()
            ?? throw new InvalidOperationException($"Hiányzik a '{KeycloakOptions.SectionName}' konfiguráció.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = keycloak.Authority;
                if (!string.IsNullOrWhiteSpace(keycloak.MetadataAddress))
                {
                    options.MetadataAddress = keycloak.MetadataAddress;
                }

                options.Audience = keycloak.Audience;
                options.RequireHttpsMetadata = keycloak.RequireHttpsMetadata;
                options.MapInboundClaims = false;
                options.TokenValidationParameters.ValidIssuer = keycloak.Authority;
                options.TokenValidationParameters.NameClaimType = "preferred_username";
                options.TokenValidationParameters.RoleClaimType = RoleClaimType;
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        if (context.Principal?.Identity is ClaimsIdentity identity)
                        {
                            AddRealmRoles(identity);
                        }

                        return Task.CompletedTask;
                    },
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.Runner, p => p.RequireRole(Roles.Runner))
            .AddPolicy(Policies.Coach, p => p.RequireRole(Roles.Coach))
            .AddPolicy(Policies.Admin, p => p.RequireRole(Roles.Admin))
            .AddPolicy(Policies.RunnerOrCoach, p => p.RequireRole(Roles.Runner, Roles.Coach));

        return services;
    }

    /// <summary>
    /// A Keycloak a realm szerepköröket a <c>realm_access.roles</c> JSON tömbben adja;
    /// ezeket külön role claim-ekké alakítjuk, hogy az <c>IsInRole</c> és a policy-k működjenek.
    /// </summary>
    internal static void AddRealmRoles(ClaimsIdentity identity)
    {
        var realmAccess = identity.FindFirst("realm_access")?.Value;
        if (string.IsNullOrEmpty(realmAccess))
        {
            return;
        }

        using var document = JsonDocument.Parse(realmAccess);
        if (!document.RootElement.TryGetProperty("roles", out var roles) || roles.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var role in roles.EnumerateArray())
        {
            if (role.GetString() is { Length: > 0 } name && !identity.HasClaim(RoleClaimType, name))
            {
                identity.AddClaim(new Claim(RoleClaimType, name));
            }
        }
    }
}
