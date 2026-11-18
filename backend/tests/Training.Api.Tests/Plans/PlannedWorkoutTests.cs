using Contracts;
using Contracts.Training;
using Training.Api.Plans;

namespace Training.Api.Tests.Plans;

/// <summary>
/// A tervezett edzés állapotátmeneteinek tesztjei: tervezett → teljesített / kihagyott.
/// </summary>
public class PlannedWorkoutTests
{
    private static readonly DateOnly Today = new(2026, 11, 18);

    /// <summary>
    /// Új tervezett edzés a terv futójához tartozik, és tervezett állapotú.
    /// </summary>
    [Fact]
    public void Create_IsPlannedForPlanRunner()
    {
        var plan = NewPlan();

        var plannedWorkout = PlannedWorkout.Create(plan, Input(Today.AddDays(1)));

        Assert.Equal(PlannedWorkoutStatus.Planned, plannedWorkout.Status);
        Assert.Equal(plan.RunnerId, plannedWorkout.RunnerId);
        Assert.Equal(plan.Id, plannedWorkout.PlanId);
    }

    /// <summary>
    /// Teljesítéskor megjegyzi az edzést; az edzés törlésekor újra tervezett lesz.
    /// </summary>
    [Fact]
    public void CompleteAndReopen()
    {
        var plannedWorkout = PlannedWorkout.Create(NewPlan(), Input(Today));
        var workoutId = Guid.NewGuid();

        plannedWorkout.Complete(workoutId);
        Assert.Equal(PlannedWorkoutStatus.Completed, plannedWorkout.Status);
        Assert.Equal(workoutId, plannedWorkout.WorkoutId);

        plannedWorkout.Reopen();
        Assert.Equal(PlannedWorkoutStatus.Planned, plannedWorkout.Status);
        Assert.Null(plannedWorkout.WorkoutId);
    }

    /// <summary>
    /// Egy kihagyott edzés mai vagy későbbi napra áthelyezve újra tervezett lesz, múltbeli napra nem.
    /// </summary>
    [Theory]
    [InlineData(-1, PlannedWorkoutStatus.Missed)]
    [InlineData(0, PlannedWorkoutStatus.Planned)]
    [InlineData(3, PlannedWorkoutStatus.Planned)]
    public void MovingMissedWorkout_ReplansOnlyForTodayOrLater(int newDayOffset, PlannedWorkoutStatus expectedStatus)
    {
        var plannedWorkout = PlannedWorkout.Create(NewPlan(), Input(Today.AddDays(-2)));
        plannedWorkout.MarkMissed();

        plannedWorkout.Update(Input(Today.AddDays(newDayOffset)), Today);

        Assert.Equal(expectedStatus, plannedWorkout.Status);
    }

    /// <summary>
    /// Teljesített edzés áthelyezése nem változtat az állapotán.
    /// </summary>
    [Fact]
    public void MovingCompletedWorkout_StaysCompleted()
    {
        var plannedWorkout = PlannedWorkout.Create(NewPlan(), Input(Today));
        plannedWorkout.Complete(Guid.NewGuid());

        plannedWorkout.Update(Input(Today.AddDays(1)), Today);

        Assert.Equal(PlannedWorkoutStatus.Completed, plannedWorkout.Status);
    }

    /// <summary>
    /// Egy tesztterv egy véletlen futónak, egy hónapra.
    /// </summary>
    private static TrainingPlan NewPlan() =>
        TrainingPlan.Create(Guid.NewGuid(), Guid.NewGuid(), new ValidPlanInput("Terv", Today.AddDays(-10), Today.AddDays(20), false), null, DateTimeOffset.UtcNow);

    /// <summary>
    /// Egy könnyű tervezett edzés a megadott napra.
    /// </summary>
    private static ValidPlannedWorkoutInput Input(DateOnly date) => new(date, WorkoutType.Easy, 8m, null, 3, null);
}
