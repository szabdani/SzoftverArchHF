using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.CoachAccess;

/// <summary>
/// Az Identity szolgáltatásban élő edző–futó kapcsolat helyi másolata. Minden szolgáltatás,
/// amely futói adatot ad ki, ebből ellenőrzi az edzői hozzáférést, szinkron hívás nélkül.
/// </summary>
public class CoachRunnerLink
{
    public Guid CoachId { get; set; }
    public Guid RunnerId { get; set; }
    public DateTimeOffset LinkedAt { get; set; }
}

public interface ICoachRunnerLinkStore
{
    DbSet<CoachRunnerLink> CoachRunnerLinks { get; }
}

public static class CoachRunnerLinkModelBuilderExtensions
{
    public static ModelBuilder AddCoachRunnerLinks(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CoachRunnerLink>(link =>
        {
            link.HasKey(x => new { x.CoachId, x.RunnerId });
            link.HasIndex(x => x.RunnerId);
        });
        return modelBuilder;
    }
}
