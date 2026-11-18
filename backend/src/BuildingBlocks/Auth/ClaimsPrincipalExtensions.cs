using System.Security.Claims;

namespace BuildingBlocks.Auth;

public static class ClaimsPrincipalExtensions
{
    /// <summary>A Keycloak felhasználó azonosítója (<c>sub</c> claim).</summary>
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id)
            ? id
            : throw new InvalidOperationException("A token nem tartalmaz érvényes 'sub' claim-et.");
    }

    /// <summary>A bejelentkezett felhasználó e-mail címe (a token <c>email</c> claim-je), ha van.</summary>
    public static string? GetEmail(this ClaimsPrincipal user) => user.FindFirst("email")?.Value;
}
