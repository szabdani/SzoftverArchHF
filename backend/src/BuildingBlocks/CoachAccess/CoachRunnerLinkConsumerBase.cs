using Contracts.Identity;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.CoachAccess;

/// <summary>
/// Az Identity eseményeiből karbantartja a helyi kapcsolattáblát. Minden szolgáltatás
/// leszármaztat belőle egy saját consumert, így mindegyik saját queue-t kap.
/// </summary>
public abstract class CoachRunnerLinkConsumerBase<TDbContext>(TDbContext db)
    : IConsumer<CoachRunnerLinked>, IConsumer<CoachRunnerUnlinked>
    where TDbContext : DbContext, ICoachRunnerLinkStore
{
    public async Task Consume(ConsumeContext<CoachRunnerLinked> context)
    {
        var message = context.Message;
        var existing = await db.CoachRunnerLinks.FindAsync(
            [message.CoachId, message.RunnerId], context.CancellationToken);

        if (existing is null)
        {
            db.CoachRunnerLinks.Add(new CoachRunnerLink
            {
                CoachId = message.CoachId,
                RunnerId = message.RunnerId,
                LinkedAt = message.OccurredAt,
            });
        }
        else if (existing.LinkedAt < message.OccurredAt)
        {
            existing.LinkedAt = message.OccurredAt;
        }

        await db.SaveChangesAsync(context.CancellationToken);
    }

    public async Task Consume(ConsumeContext<CoachRunnerUnlinked> context)
    {
        var message = context.Message;
        var existing = await db.CoachRunnerLinks.FindAsync(
            [message.CoachId, message.RunnerId], context.CancellationToken);

        // Egy késve érkező bontás ne töröljön egy azóta újra létrejött kapcsolatot.
        if (existing is not null && existing.LinkedAt <= message.OccurredAt)
        {
            db.CoachRunnerLinks.Remove(existing);
            await db.SaveChangesAsync(context.CancellationToken);
        }
    }
}
