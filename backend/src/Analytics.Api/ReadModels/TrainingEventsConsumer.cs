using Analytics.Api.Persistence;
using Analytics.Api.Risk;
using Contracts.Demo;
using Contracts.Training;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Analytics.Api.ReadModels;

/// <summary>
/// A Training szolgáltatás eseményeiből karbantartja az edzések, tervezett edzések és az aktív cél helyi
/// másolatát, majd az edzés-eseményeknél újraszámolja a futó kockázati pontszámát (UC20).
/// </summary>
public class TrainingEventsConsumer(AnalyticsDbContext db, IRiskService riskService) :
    IConsumer<WorkoutRecorded>,
    IConsumer<WorkoutUpdated>,
    IConsumer<WorkoutDeleted>,
    IConsumer<PlannedWorkoutChanged>,
    IConsumer<PlannedWorkoutRemoved>,
    IConsumer<RunnerGoalChanged>,
    IConsumer<RunnerDataReset>
{
    /// <summary>
    /// Új edzés: felvétel a helyi másolatba, majd újraszámolás.
    /// </summary>
    public Task Consume(ConsumeContext<WorkoutRecorded> context) =>
        UpsertWorkoutAsync(context.Message.Workout, context.CancellationToken);

    /// <summary>
    /// Módosított edzés: frissítés a helyi másolatban, majd újraszámolás.
    /// </summary>
    public Task Consume(ConsumeContext<WorkoutUpdated> context) =>
        UpsertWorkoutAsync(context.Message.Workout, context.CancellationToken);

    /// <summary>
    /// Törölt edzés: törlés a helyi másolatból, majd újraszámolás.
    /// </summary>
    public async Task Consume(ConsumeContext<WorkoutDeleted> context)
    {
        var message = context.Message;
        var record = await db.Workouts.FindAsync([message.WorkoutId], context.CancellationToken);
        if (record is not null)
        {
            db.Workouts.Remove(record);
            await db.SaveChangesAsync(context.CancellationToken);
        }

        await riskService.RecalculateAsync(message.RunnerId, context.CancellationToken);
    }

    /// <summary>
    /// Új vagy megváltozott tervezett edzés (pl. kihagyottra váltott): frissítés, majd újraszámolás.
    /// </summary>
    public async Task Consume(ConsumeContext<PlannedWorkoutChanged> context)
    {
        var snapshot = context.Message.PlannedWorkout;
        var record = await db.PlannedWorkouts.FindAsync([snapshot.PlannedWorkoutId], context.CancellationToken);
        if (record is null)
        {
            record = new PlannedWorkoutRecord { Id = snapshot.PlannedWorkoutId };
            db.PlannedWorkouts.Add(record);
        }

        record.RunnerId = snapshot.RunnerId;
        record.Date = snapshot.Date;
        record.Type = snapshot.Type;
        record.PlannedDistanceKm = snapshot.PlannedDistanceKm;
        record.PlannedDurationSeconds = snapshot.PlannedDurationSeconds;
        record.TargetRpe = snapshot.TargetRpe;
        record.Status = snapshot.Status;
        await db.SaveChangesAsync(context.CancellationToken);

        await riskService.RecalculateAsync(snapshot.RunnerId, context.CancellationToken);
    }

    /// <summary>
    /// Törölt tervezett edzés: törlés a helyi másolatból, majd újraszámolás.
    /// </summary>
    public async Task Consume(ConsumeContext<PlannedWorkoutRemoved> context)
    {
        var message = context.Message;
        var record = await db.PlannedWorkouts.FindAsync([message.PlannedWorkoutId], context.CancellationToken);
        if (record is not null)
        {
            db.PlannedWorkouts.Remove(record);
            await db.SaveChangesAsync(context.CancellationToken);
        }

        await riskService.RecalculateAsync(message.RunnerId, context.CancellationToken);
    }

    /// <summary>
    /// Új vagy módosított aktív cél: a futó célja a helyi másolatban felülíródik (a javaslathoz kell).
    /// </summary>
    public async Task Consume(ConsumeContext<RunnerGoalChanged> context)
    {
        var message = context.Message;
        var record = await db.ActiveGoals.FindAsync([message.RunnerId], context.CancellationToken);
        if (record is null)
        {
            record = new ActiveGoalRecord { RunnerId = message.RunnerId };
            db.ActiveGoals.Add(record);
        }

        record.Type = message.Type;
        record.DistanceKm = message.DistanceKm;
        record.TargetDate = message.TargetDate;
        await db.SaveChangesAsync(context.CancellationToken);
    }

    /// <summary>
    /// A futó Training-adatai törlődtek (demó): minden róla tárolt adat törlődik, a figyelmeztetések és
    /// a pontszámok is. Mivel ez a sor egyszerre egy üzenetet dolgoz fel, a törlés biztosan az utána
    /// küldött új adatok előtt fut le.
    /// </summary>
    public async Task Consume(ConsumeContext<RunnerDataReset> context)
    {
        var runnerId = context.Message.RunnerId;
        var ct = context.CancellationToken;

        await db.Workouts.Where(workout => workout.RunnerId == runnerId).ExecuteDeleteAsync(ct);
        await db.PlannedWorkouts.Where(plannedWorkout => plannedWorkout.RunnerId == runnerId).ExecuteDeleteAsync(ct);
        await db.ActiveGoals.Where(goal => goal.RunnerId == runnerId).ExecuteDeleteAsync(ct);
        await db.RiskScoreSnapshots.Where(snapshot => snapshot.RunnerId == runnerId).ExecuteDeleteAsync(ct);
        await db.Alerts.Where(alert => alert.RunnerId == runnerId).ExecuteDeleteAsync(ct);
    }

    /// <summary>
    /// Edzés felvétele vagy frissítése a helyi másolatban (a sorrendtől függetlenül), majd újraszámolás.
    /// </summary>
    private async Task UpsertWorkoutAsync(WorkoutSnapshot snapshot, CancellationToken ct)
    {
        var record = await db.Workouts.FindAsync([snapshot.WorkoutId], ct);
        if (record is null)
        {
            record = new WorkoutRecord { Id = snapshot.WorkoutId };
            db.Workouts.Add(record);
        }

        record.RunnerId = snapshot.RunnerId;
        record.Date = snapshot.Date;
        record.Type = snapshot.Type;
        record.DistanceKm = snapshot.DistanceKm;
        record.DurationSeconds = snapshot.DurationSeconds;
        record.Rpe = snapshot.Rpe;
        record.ElevationGainM = snapshot.ElevationGainM;
        record.Load = snapshot.Load;
        await db.SaveChangesAsync(ct);

        await riskService.RecalculateAsync(snapshot.RunnerId, ct);
    }
}
