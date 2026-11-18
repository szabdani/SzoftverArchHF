using Contracts.Demo;
using Contracts.Identity;
using Identity.Api.Invitations;
using Identity.Api.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Identity.Api.Demo;

/// <summary>
/// A demó indítása: a kapcsolatok itt jönnek létre (ugyanúgy, mint egy elfogadott meghívásnál),
/// az edzésadatokat pedig a Training szolgáltatás generálja a <see cref="DemoDataRequested"/> eseményből.
/// </summary>
public class DemoService(IdentityDbContext db, IPublishEndpoint publisher, IOptions<DemoOptions> options, TimeProvider time) : IDemoService
{
    private readonly DemoOptions demo = options.Value;

    /// <summary>
    /// Összekapcsolja a demó edzőt a szimulált futókkal, és kéri a 12 hét adat legenerálását.
    /// </summary>
    public async Task<List<DemoRunnerResponse>> CreateAsync(CancellationToken ct)
    {
        var now = time.GetUtcNow();
        foreach (var runner in demo.Runners)
        {
            // Ha a kapcsolat már él (ismételt generálás), nem hozunk létre újat.
            var linked = await db.Invitations.AnyAsync(invitation =>
                invitation.CoachId == demo.CoachId
                && invitation.RunnerId == runner.RunnerId
                && invitation.Status == InvitationStatus.Accepted, ct);

            if (!linked)
            {
                var invitation = Invitation.Create(demo.CoachId, demo.CoachEmail, runner.Email, now);
                invitation.Accept(runner.RunnerId, now);
                db.Invitations.Add(invitation);
                await publisher.Publish(new CoachRunnerLinked(demo.CoachId, runner.RunnerId, now), ct);
            }
        }

        var runners = demo.Runners.Select(runner => new DemoRunner(runner.RunnerId, runner.Pattern)).ToList();
        await publisher.Publish(new DemoDataRequested(runners, now), ct);
        await db.SaveChangesAsync(ct);

        return demo.Runners.Select(runner => new DemoRunnerResponse(runner.RunnerId, runner.Email, runner.Pattern)).ToList();
    }

    /// <summary>
    /// Megszünteti a demó kapcsolatokat, és kéri a szimulált futók adatainak törlését.
    /// </summary>
    public async Task RemoveAsync(CancellationToken ct)
    {
        var now = time.GetUtcNow();
        var runnerIds = demo.Runners.Select(runner => runner.RunnerId).ToList();

        // A demó edző élő kapcsolatai a szimulált futókkal.
        var links = await db.Invitations
            .Where(invitation => invitation.CoachId == demo.CoachId
                && invitation.Status == InvitationStatus.Accepted
                && invitation.RunnerId != null
                && runnerIds.Contains(invitation.RunnerId.Value))
            .ToListAsync(ct);

        foreach (var link in links)
        {
            link.End(now);
            await publisher.Publish(new CoachRunnerUnlinked(link.CoachId, link.RunnerId!.Value, now), ct);
        }

        await publisher.Publish(new DemoDataRemoved(runnerIds, now), ct);
        await db.SaveChangesAsync(ct);
    }
}
