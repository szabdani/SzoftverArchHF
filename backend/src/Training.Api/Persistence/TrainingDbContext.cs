using BuildingBlocks.CoachAccess;
using BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore;
using Training.Api.Goals;
using Training.Api.Plans;
using Training.Api.Workouts;

namespace Training.Api.Persistence;

/// <summary>
/// A Training szolgáltatás adatbázisa: edzések, célok, edzéstervek és az edző–futó kapcsolatok helyi másolata.
/// </summary>
public class TrainingDbContext(DbContextOptions<TrainingDbContext> options) : DbContext(options), ICoachRunnerLinkStore
{
    public DbSet<Workout> Workouts => Set<Workout>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<TrainingPlan> TrainingPlans => Set<TrainingPlan>();
    public DbSet<PlannedWorkout> PlannedWorkouts => Set<PlannedWorkout>();
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

        modelBuilder.Entity<TrainingPlan>(plan =>
        {
            plan.HasKey(x => x.Id);
            plan.Property(x => x.Name).HasMaxLength(TrainingPlan.NameMaxLength);
            plan.HasIndex(x => x.RunnerId);
            plan.HasMany(x => x.Workouts).WithOne().HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlannedWorkout>(plannedWorkout =>
        {
            plannedWorkout.HasKey(x => x.Id);
            plannedWorkout.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
            plannedWorkout.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            plannedWorkout.Property(x => x.PlannedDistanceKm).HasPrecision(5, 2);
            plannedWorkout.Property(x => x.Note).HasMaxLength(PlannedWorkout.NoteMaxLength);
            plannedWorkout.HasIndex(x => new { x.RunnerId, x.Date });
        });

        modelBuilder.AddCoachRunnerLinks();
        modelBuilder.AddMessagingOutbox();
    }
}
