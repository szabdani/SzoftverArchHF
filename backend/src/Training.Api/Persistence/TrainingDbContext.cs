using BuildingBlocks.CoachAccess;
using BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore;
using Training.Api.Workouts;

namespace Training.Api.Persistence;

public class TrainingDbContext(DbContextOptions<TrainingDbContext> options) : DbContext(options), ICoachRunnerLinkStore
{
    public DbSet<Workout> Workouts => Set<Workout>();
    public DbSet<CoachRunnerLink> CoachRunnerLinks => Set<CoachRunnerLink>();

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

        modelBuilder.AddCoachRunnerLinks();
        modelBuilder.AddMessagingOutbox();
    }
}
