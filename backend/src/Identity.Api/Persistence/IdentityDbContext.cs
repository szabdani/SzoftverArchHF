using BuildingBlocks.Persistence;
using Identity.Api.Invitations;
using Microsoft.EntityFrameworkCore;

namespace Identity.Api.Persistence;

/// <summary>
/// Az Identity szolgáltatás adatbázisa: edzői meghívások és edző–futó kapcsolatok.
/// </summary>
public class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    public DbSet<Invitation> Invitations => Set<Invitation>();

    /// <summary>
    /// A meghívás tábla leképezése és a MassTransit outbox táblái.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Invitation>(invitation =>
        {
            invitation.HasKey(x => x.Id);
            invitation.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            invitation.Property(x => x.CoachEmail).HasMaxLength(Invitation.EmailMaxLength);
            invitation.Property(x => x.RunnerEmail).HasMaxLength(Invitation.EmailMaxLength);
            invitation.HasIndex(x => x.CoachId);
            invitation.HasIndex(x => x.RunnerId);
            invitation.HasIndex(x => x.RunnerEmail);
        });

        modelBuilder.AddMessagingOutbox();
    }
}
