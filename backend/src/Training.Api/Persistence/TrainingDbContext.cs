using BuildingBlocks.CoachAccess;
using BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore;
using Training.Api.Goals;
using Training.Api.Workouts;

namespace Training.Api.Persistence;

/// <summary>
/// A Training szolgáltatás adatbázisa: edzések, célok és az edző–futó kapcsolatok helyi másolata.
/// </summary>
public class TrainingDbContext(DbContextOptions<TrainingDbContext> options) : DbContext(options), ICoachRunnerLinkStore
{
    public DbSet<Workout> Workouts => Set<Workout>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<CoachRunnerLink> CoachRunnerLinks => Set<CoachRunnerLink>();

    /// <summary>
    /// A táblák leképezése és a MassTransit outbox táblái.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Workout>(workout =>
        {
            workout.HasKey(x => x.Id);
            workout.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
            workout.Property(x => x.DistanceKm).HasPrecision(5, 2);
            workout.Property(x => x.Note).HasMaxLength(Workout.NoteMaxLength);
            workout.HasIndex(x => new { x.RunnerId, x.Date });
        });

        modelBuilder.Entity<Goal>(goal =>
        {
            goal.HasKey(x => x.Id);
            goal.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
            goal.Property(x => x.DistanceKm).HasPrecision(7, 4);
            goal.HasIndex(x => x.RunnerId);
        });

        modelBuilder.AddCoachRunnerLinks();
        modelBuilder.AddMessagingOutbox();
    }
}
