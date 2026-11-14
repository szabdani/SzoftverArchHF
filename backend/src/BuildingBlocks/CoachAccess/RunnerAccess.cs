using System.Security.Claims;
using BuildingBlocks.Auth;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.CoachAccess;

public static class RunnerAccess
{
    /// <summary>
    /// Láthatja-e a felhasználó a futó adatait: a futó a sajátját, az edző csak a hozzá rendelt
    /// futókét. Az adminisztrátor szándékosan nem (spec 3.1).
    /// </summary>
    public static async Task<bool> CanViewRunnerAsync(
        this ICoachRunnerLinkStore store, ClaimsPrincipal user, Guid runnerId, CancellationToken ct)
    {
        var userId = user.GetUserId();
        if (userId == runnerId && user.IsInRole(Roles.Runner))
        {
            return true;
        }

        return user.IsInRole(Roles.Coach)
            && await store.CoachRunnerLinks.AnyAsync(l => l.CoachId == userId && l.RunnerId == runnerId, ct);
    }
}
