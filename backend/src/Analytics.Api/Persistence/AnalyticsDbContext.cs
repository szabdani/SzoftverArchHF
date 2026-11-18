using Analytics.Api.Alerts;
using Analytics.Api.ReadModels;
using Analytics.Api.Risk;
using Analytics.Api.Rules;
using BuildingBlocks.CoachAccess;
using BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Analytics.Api.Persistence;

/// <summary>
/// Az Analytics szolgáltatás adatbázisa: az edzések, tervezett edzések és aktív célok helyi másolata,
/// az edző–futó kapcsolatok, a kiszámolt kockázati pontszámok és a figyelmeztetések.
/// </summary>
public class AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options) : DbContext(options), ICoachRunnerLinkStore
{
    public DbSet<WorkoutRecord> Workouts => Set<WorkoutRecord>();
    public DbSet<PlannedWorkoutRecord> PlannedWorkouts => Set<PlannedWorkoutRecord>();
    public DbSet<RiskScoreSnapshot> RiskScoreSnapshots => Set<RiskScoreSnapshot>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<ActiveGoalRecord> ActiveGoals => Set<ActiveGoalRecord>();
    public DbSet<RuleSettings> RuleSettings => Set<RuleSettings>();
    public DbSet<CoachRunnerLink> CoachRunnerLinks => Set<CoachRunnerLink>();

    /// <summary>
    /// A táblák leképezése és a MassTransit outbox/inbox táblái.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // A read model azonosítói a Training szolgáltatásból jönnek, ezért nem generáljuk őket.
        modelBuilder.Entity<WorkoutRecord>(workout =>
        {
            workout.HasKey(x => x.Id);
            workout.Property(x => x.Id).ValueGeneratedNever();
            workout.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
            workout.Property(x => x.DistanceKm).HasPrecision(5, 2);
            workout.HasIndex(x => new { x.RunnerId, x.Date });
        });

        modelBuilder.Entity<PlannedWorkoutRecord>(plannedWorkout =>
        {
            plannedWorkout.HasKey(x => x.Id);
            plannedWorkout.Property(x => x.Id).ValueGeneratedNever();
            plannedWorkout.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
            plannedWorkout.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            plannedWorkout.Property(x => x.PlannedDistanceKm).HasPrecision(5, 2);
            plannedWorkout.HasIndex(x => new { x.RunnerId, x.Date });
        });

        modelBuilder.Entity<ActiveGoalRecord>(goal =>
        {
            goal.HasKey(x => x.RunnerId);
            goal.Property(x => x.RunnerId).ValueGeneratedNever();
            goal.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
            goal.Property(x => x.DistanceKm).HasPrecision(7, 4);
        });

        modelBuilder.Entity<RiskScoreSnapshot>(snapshot =>
        {
            snapshot.HasKey(x => x.Id);
            snapshot.Property(x => x.Category).HasConversion<string>().HasMaxLength(20);
            snapshot.Property(x => x.WeeklyKm).HasPrecision(7, 2);
            snapshot.Property(x => x.PreviousWeeklyKm).HasPrecision(7, 2);
            snapshot.HasIndex(x => new { x.RunnerId, x.Date }).IsUnique();
        });

        modelBuilder.Entity<Alert>(alert =>
        {
            alert.HasKey(x => x.Id);
            alert.Property(x => x.Rule).HasConversion<string>().HasMaxLength(40);
            alert.Property(x => x.Severity).HasConversion<string>().HasMaxLength(20);
            alert.Property(x => x.Message).HasMaxLength(300);
            alert.Property(x => x.PeriodKey).HasMaxLength(60);

            // Ugyanaz a szabály ugyanarra az időszakra egyszer (spec 3.6).
            alert.HasIndex(x => new { x.RunnerId, x.Rule, x.PeriodKey }).IsUnique();
        });

        modelBuilder.Entity<RuleSettings>(settings =>
        {
            settings.HasKey(x => x.Id);
            settings.Property(x => x.Id).ValueGeneratedNever();
        });

        modelBuilder.AddCoachRunnerLinks();
        modelBuilder.AddMessagingOutbox();
    }
}
