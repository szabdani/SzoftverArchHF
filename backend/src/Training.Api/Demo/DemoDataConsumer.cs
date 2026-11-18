using BuildingBlocks.Time;
using Contracts.Demo;
using Contracts.Training;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Training.Api.Goals;
using Training.Api.Persistence;
using Training.Api.Plans;
using Training.Api.Workouts;

namespace Training.Api.Demo;

/// <summary>
/// A demóadatok létrehozása és törlése (UC18) az Identity eseményeiből. Minden létrehozott és törölt
/// adatról ugyanaz az esemény megy ki, mint a normál használatnál, így az Analytics is feldolgozza.
/// </summary>
public class DemoDataConsumer(TrainingDbContext db, TimeProvider time) : IConsumer<DemoDataRequested>, IConsumer<DemoDataRemoved>
{
    /// <summary>
    /// A szimulált futók adatainak (újra)generálása: előbb a korábbiak törlődnek.
    /// </summary>
    public async Task Consume(ConsumeContext<DemoDataRequested> context)
    {
        var today = time.GetLocalToday();
        var now = time.GetUtcNow();

        foreach (var runner in context.Message.Runners)
        {
            await RemoveRunnerDataAsync(context, runner.RunnerId, now);

            var data = DemoDataGenerator.Generate(runner.Pattern, today);
            var goal = Goal.Create(runner.RunnerId, data.Goal, now);
            var plan = TrainingPlan.Create(runner.RunnerId, runner.RunnerId, data.Plan, goal.Id, now);
            db.Goals.Add(goal);
            db.TrainingPlans.Add(plan);

            foreach (var demo in data.PlannedWorkouts)
            {
                var plannedWorkout = PlannedWorkout.Create(plan, demo.Planned);
                db.PlannedWorkouts.Add(plannedWorkout);

                // Az elvégzett edzés a tervezetthez kötve (UC08), a ki nem végzett múltbeli kihagyott.
                if (demo.Workout is { } workoutInput)
                {
                    var workout = Workout.Create(runner.RunnerId, workoutInput with { PlannedWorkoutId = plannedWorkout.Id }, now);
                    db.Workouts.Add(workout);
                    plannedWorkout.Complete(workout.Id);
                    await context.Publish(new WorkoutRecorded(workout.ToSnapshot(), now));
                }
                else if (demo.Missed)
                {
                    plannedWorkout.MarkMissed();
                }

                await context.Publish(new PlannedWorkoutChanged(plannedWorkout.ToSnapshot(), now));
            }

            await context.Publish(new RunnerGoalChanged(runner.RunnerId, goal.Type, goal.DistanceKm, goal.TargetDate, now));
        }

        await db.SaveChangesAsync(context.CancellationToken);
    }

    /// <summary>
    /// A szimulált futók adatainak törlése.
    /// </summary>
    public async Task Consume(ConsumeContext<DemoDataRemoved> context)
    {
        var now = time.GetUtcNow();
        foreach (var runnerId in context.Message.RunnerIds)
        {
            await RemoveRunnerDataAsync(context, runnerId, now);
        }

        await db.SaveChangesAsync(context.CancellationToken);
    }

    /// <summary>
    /// A futó összes edzése, terve (a tervezett edzésekkel) és célja törlésre jelölődik, és egyetlen
    /// <see cref="RunnerDataReset"/> esemény megy ki róla.
    /// </summary>
    private async Task RemoveRunnerDataAsync(ConsumeContext context, Guid runnerId, DateTimeOffset now)
    {
        var ct = context.CancellationToken;

        // A tervezett edzések kaszkádolva törlődnek a tervvel.
        db.Workouts.RemoveRange(await db.Workouts.Where(workout => workout.RunnerId == runnerId).ToListAsync(ct));
        db.TrainingPlans.RemoveRange(await db.TrainingPlans.Include(plan => plan.Workouts).Where(plan => plan.RunnerId == runnerId).ToListAsync(ct));
        db.Goals.RemoveRange(await db.Goals.Where(goal => goal.RunnerId == runnerId).ToListAsync(ct));

        await context.Publish(new RunnerDataReset(runnerId, now));
    }
}
