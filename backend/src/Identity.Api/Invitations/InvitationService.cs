using System.Security.Claims;
using BuildingBlocks.Auth;
using Contracts.Identity;
using Identity.Api.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Identity.Api.Invitations;

/// <summary>
/// A meghívások és kapcsolatok üzleti logikája. A kapcsolat létrejöttéről és megszűnéséről
/// eseményt küld, amiből a többi szolgáltatás frissíti az edzői hozzáférést.
/// </summary>
public class InvitationService(IdentityDbContext db, IPublishEndpoint publisher, TimeProvider time) : IInvitationService
{
    /// <summary>
    /// Új meghívás a bejelentkezett edzőtől a megadott, már ellenőrzött e-mail címre.
    /// </summary>
    /// <returns>A meghívás, vagy null, ha erre a címre már van függő meghívás vagy élő kapcsolat.</returns>
    public async Task<InvitationResponse?> CreateAsync(ClaimsPrincipal coach, string runnerEmail, CancellationToken ct)
    {
        var coachId = coach.GetUserId();

        // Ugyanarra a címre egyszerre csak egy függő meghívás vagy élő kapcsolat lehet;
        // elutasított vagy megszüntetett kapcsolat után újra lehet hívni.
        var alreadyInvited = await db.Invitations.AnyAsync(invitation =>
            invitation.CoachId == coachId
            && invitation.RunnerEmail == runnerEmail
            && (invitation.Status == InvitationStatus.Pending || invitation.Status == InvitationStatus.Accepted), ct);

        if (alreadyInvited)
        {
            return null;
        }

        var coachEmail = InvitationValidator.NormalizeEmail(coach.GetEmail());
        var newInvitation = Invitation.Create(coachId, coachEmail, runnerEmail, time.GetUtcNow());
        db.Invitations.Add(newInvitation);
        await db.SaveChangesAsync(ct);

        return InvitationResponse.From(newInvitation);
    }

    /// <summary>
    /// Az edző által küldött összes meghívás, a legfrissebb elöl.
    /// </summary>
    public async Task<List<InvitationResponse>> ListSentAsync(ClaimsPrincipal coach, CancellationToken ct)
    {
        var coachId = coach.GetUserId();

        var invitations = await db.Invitations
            .Where(invitation => invitation.CoachId == coachId)
            .OrderByDescending(invitation => invitation.CreatedAt)
            .ToListAsync(ct);

        return invitations.Select(InvitationResponse.From).ToList();
    }

    /// <summary>
    /// A futó e-mail címére érkezett, még megválaszolatlan meghívások.
    /// </summary>
    public async Task<List<InvitationResponse>> ListReceivedAsync(ClaimsPrincipal runner, CancellationToken ct)
    {
        var email = InvitationValidator.NormalizeEmail(runner.GetEmail());

        // A meghívás e-mail címre szól, mert az edző a futó azonosítóját nem ismeri.
        var invitations = await db.Invitations
            .Where(invitation => invitation.RunnerEmail == email && invitation.Status == InvitationStatus.Pending)
            .OrderByDescending(invitation => invitation.CreatedAt)
            .ToListAsync(ct);

        return invitations.Select(InvitationResponse.From).ToList();
    }

    /// <summary>
    /// A futó elfogadja a meghívást: létrejön a kapcsolat, és CoachRunnerLinked esemény megy ki.
    /// </summary>
    /// <returns>Az elfogadott meghívás, vagy null, ha nincs ilyen függő meghívás a futó címére.</returns>
    public async Task<InvitationResponse?> AcceptAsync(ClaimsPrincipal runner, Guid id, CancellationToken ct)
    {
        var invitation = await FindPendingForRunnerAsync(runner, id, ct);
        if (invitation is null)
        {
            return null;
        }

        var runnerId = runner.GetUserId();
        var now = time.GetUtcNow();
        invitation.Accept(runnerId, now);

        // A Publish a SaveChanges előtt kell: az outbox így ugyanabban a tranzakcióban menti az eseményt.
        await publisher.Publish(new CoachRunnerLinked(invitation.CoachId, runnerId, now), ct);
        await db.SaveChangesAsync(ct);

        return InvitationResponse.From(invitation);
    }

    /// <summary>
    /// A futó elutasítja a meghívást.
    /// </summary>
    /// <returns>true, ha elutasította; false, ha nincs ilyen függő meghívás a futó címére.</returns>
    public async Task<bool> DeclineAsync(ClaimsPrincipal runner, Guid id, CancellationToken ct)
    {
        var invitation = await FindPendingForRunnerAsync(runner, id, ct);
        if (invitation is null)
        {
            return false;
        }

        invitation.Decline(time.GetUtcNow());
        await db.SaveChangesAsync(ct);

        return true;
    }

    /// <summary>
    /// A felhasználó élő kapcsolatai: futónál az edzői, edzőnél a futói.
    /// </summary>
    public async Task<List<InvitationResponse>> ListLinksAsync(ClaimsPrincipal user, CancellationToken ct)
    {
        var userId = user.GetUserId();

        // Csak az elfogadott meghívások élő kapcsolatok; a felhasználó bármelyik oldalon állhat.
        var links = await db.Invitations
            .Where(invitation => invitation.Status == InvitationStatus.Accepted
                && (invitation.CoachId == userId || invitation.RunnerId == userId))
            .OrderByDescending(invitation => invitation.UpdatedAt)
            .ToListAsync(ct);

        return links.Select(InvitationResponse.From).ToList();
    }

    /// <summary>
    /// A futó megszünteti egy élő kapcsolatát, és CoachRunnerUnlinked esemény megy ki.
    /// </summary>
    /// <returns>true, ha megszüntette; false, ha nincs ilyen élő kapcsolata.</returns>
    public async Task<bool> EndLinkAsync(ClaimsPrincipal runner, Guid id, CancellationToken ct)
    {
        var runnerId = runner.GetUserId();
        var link = await db.Invitations.FirstOrDefaultAsync(invitation =>
            invitation.Id == id
            && invitation.RunnerId == runnerId
            && invitation.Status == InvitationStatus.Accepted, ct);

        if (link is null)
        {
            return false;
        }

        var now = time.GetUtcNow();
        link.End(now);
        await publisher.Publish(new CoachRunnerUnlinked(link.CoachId, runnerId, now), ct);
        await db.SaveChangesAsync(ct);

        return true;
    }

    /// <summary>
    /// Betölti a futó e-mail címére szóló, még függő meghívást (elfogadáshoz, elutasításhoz).
    /// </summary>
    /// <returns>A meghívás, vagy null, ha nem létezik, nem a futónak szól, vagy már meg van válaszolva.</returns>
    private Task<Invitation?> FindPendingForRunnerAsync(ClaimsPrincipal runner, Guid id, CancellationToken ct)
    {
        var email = InvitationValidator.NormalizeEmail(runner.GetEmail());

        return db.Invitations.FirstOrDefaultAsync(invitation =>
            invitation.Id == id
            && invitation.RunnerEmail == email
            && invitation.Status == InvitationStatus.Pending, ct);
    }
}
