using System.Security.Claims;
using System.Text.Encodings.Web;
using BuildingBlocks.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Integration.Tests.Infrastructure;

/// <summary>
/// Teszt-hitelesítés: a valódi Keycloak JWT helyett a kérés fejléceiből állít elő egy bejelentkezett
/// felhasználót. Így az integrációs tesztek Keycloak nélkül, determinisztikusan futnak, miközben a
/// szerveroldali jogosultsági policy-k (`role` claim) és a `GetUserId` (`sub` claim) változatlanul működnek.
///
/// Fejlécek:
/// - <c>X-Test-Sub</c>: a felhasználó azonosítója (GUID), a `sub` claim.
/// - <c>X-Test-Role</c>: a szerepkör (runner / coach / admin), a `role` claim.
/// Ha nincs fejléc, a kérés hitelesítetlen marad (a védett végpont 401-et ad).
/// </summary>
public class TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";

    /// <summary>A kérés fejléceiből összeállított principal; fejléc nélkül NoResult (hitelesítetlen).</summary>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var sub = Request.Headers["X-Test-Sub"].ToString();
        var role = Request.Headers["X-Test-Role"].ToString();
        if (string.IsNullOrEmpty(sub) || string.IsNullOrEmpty(role))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        // A role-típus "role", hogy a RequireRole policy-k és az IsInRole illeszkedjen (lásd KeycloakAuthExtensions).
        var identity = new ClaimsIdentity(SchemeName, nameType: "preferred_username", roleType: KeycloakAuthExtensions.RoleClaimType);
        identity.AddClaim(new Claim("sub", sub));
        identity.AddClaim(new Claim(KeycloakAuthExtensions.RoleClaimType, role));
        identity.AddClaim(new Claim("preferred_username", sub));

        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
