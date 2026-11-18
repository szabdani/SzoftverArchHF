using Analytics.Api.ReadModels;
using Analytics.Api.Recommendations;
using Analytics.Api.Risk;
using Contracts;
using Contracts.Training;

namespace Analytics.Api.Tests.Recommendations;

/// <summary>
/// Az edzésjavaslat szabályainak tesztjei (spec 3.7): az első teljesülő szabály dönt.
/// </summary>
public class RecommendationRulesTests
{
    /// <summary>2026.11.18. szerda; a hét hétfőn (11.16.) kezdődik.</summary>
    private static readonly DateOnly Today = new(2026, 11, 18);

    /// <summary>
    /// 1. Magas kockázatnál regeneráló futás RPE 2–3, akkor is, ha tegnap intenzív nap volt és van terv.
    /// </summary>
    [Fact]
    public void HighRisk_Recovery()
    {
        var result = Recommend(RiskCategory.High, [Workout(-1, rpe: 8)], Planned(WorkoutType.Interval, 8, durationMinutes: 50));

        Assert.Equal((WorkoutType.Recovery, 2, 3), (result.Type, result.RpeMin, result.RpeMax));
    }

    /// <summary>
    /// 2. Ha tegnap intenzív nap volt, könnyű futás RPE 3–4, a mai terv ellenére is.
    /// </summary>
    [Fact]
    public void IntenseYesterday_Easy()
    {
        var result = Recommend(RiskCategory.Low, [Workout(-1, rpe: 7)], Planned(WorkoutType.Interval, 8, durationMinutes: 50));

        Assert.Equal((WorkoutType.Easy, 3, 4), (result.Type, result.RpeMin, result.RpeMax));
    }

    /// <summary>
    /// 3. Mai tervezett edzés: a terv szerint, alacsony kockázatnál változatlanul.
    /// </summary>
    [Fact]
    public void PlannedToday_AsPlanned()
    {
        var result = Recommend(RiskCategory.Low, [], Planned(WorkoutType.Tempo, 7, distanceKm: 10, durationMinutes: 50));

        Assert.Equal((WorkoutType.Tempo, 7, 7, 50, 10m), (result.Type, result.RpeMin, result.RpeMax, result.DurationMinutes, result.DistanceKm));
        Assert.Equal("A mai napra tervezett edzés: tempó.", result.Reason);
    }

    /// <summary>
    /// 3. Közepes kockázatnál eggyel alacsonyabb RPE és 20%-kal rövidebb táv és idő.
    /// </summary>
    [Fact]
    public void PlannedToday_MediumRisk_Eased()
    {
        var result = Recommend(RiskCategory.Medium, [], Planned(WorkoutType.Tempo, 7, distanceKm: 10, durationMinutes: 50));

        Assert.Equal((6, 6, 40, 8m), (result.RpeMin, result.RpeMax, result.DurationMinutes, result.DistanceKm));
        Assert.Contains("Közepes kockázat", result.Reason);
    }

    /// <summary>
    /// 3. Csak távval tervezett edzésnél az időtartam 6 perc/km-rel becsülve.
    /// </summary>
    [Fact]
    public void PlannedToday_DistanceOnly_EstimatesDuration()
    {
        var result = Recommend(RiskCategory.Low, [], Planned(WorkoutType.Easy, 3, distanceKm: 8));

        Assert.Equal(48, result.DurationMinutes);
    }

    /// <summary>
    /// 4. Legalább két napja volt az utolsó intenzív nap és alacsony a kockázat: minőségi edzés RPE 7–8;
    /// 5 és 10 km-es célnál intervall, félmaraton/maraton célnál tempó, egyéni célnál a táv dönt, cél nélkül tempó.
    /// </summary>
    [Theory]
    [InlineData(GoalType.FiveK, null, WorkoutType.Interval)]
    [InlineData(GoalType.TenK, null, WorkoutType.Interval)]
    [InlineData(GoalType.HalfMarathon, null, WorkoutType.Tempo)]
    [InlineData(GoalType.Marathon, null, WorkoutType.Tempo)]
    [InlineData(GoalType.Custom, 15.0, WorkoutType.Interval)]
    [InlineData(GoalType.Custom, 30.0, WorkoutType.Tempo)]
    [InlineData(null, null, WorkoutType.Tempo)]
    public void QualityWorkout_DependsOnGoal(GoalType? goalType, double? goalKm, WorkoutType expected)
    {
        var result = RecommendationRules.Recommend(
            RiskCategory.Low, [Workout(-2, rpe: 8)], null, goalType, (decimal?)goalKm, Today);

        Assert.Equal((expected, 7, 8), (result.Type, result.RpeMin, result.RpeMax));
    }

    /// <summary>
    /// 4. nem teljesül, ha ma már volt intenzív edzés, vagy ha a kockázat közepes; ilyenkor a hosszú futás jön.
    /// </summary>
    [Theory]
    [InlineData(RiskCategory.Low, 0)]
    [InlineData(RiskCategory.Medium, -3)]
    public void NoQualityWorkout_FallsBackToLongRun(RiskCategory category, int lastIntenseDayOffset)
    {
        var result = Recommend(category, [Workout(lastIntenseDayOffset, rpe: 8)], null);

        Assert.Equal((WorkoutType.Long, 4, 5), (result.Type, result.RpeMin, result.RpeMax));
    }

    /// <summary>
    /// 5–6. A múlt heti (vasárnapi) hosszú futás nem számít: hosszú futás jön; ha a héten már volt, könnyű futás.
    /// </summary>
    [Theory]
    [InlineData(-3, WorkoutType.Long)]
    [InlineData(-2, WorkoutType.Easy)]
    public void LongRunThisWeek(int longRunDayOffset, WorkoutType expected)
    {
        var result = Recommend(RiskCategory.Medium, [Workout(longRunDayOffset, rpe: 5, type: WorkoutType.Long)], null);

        Assert.Equal(expected, result.Type);
    }

    /// <summary>
    /// Javaslat a megadott kockázattal, edzésekkel és mai tervvel, cél nélkül.
    /// </summary>
    private static Recommendation Recommend(RiskCategory? category, IReadOnlyCollection<WorkoutRecord> workouts, PlannedWorkoutRecord? planned) =>
        RecommendationRules.Recommend(category, workouts, planned, null, null, Today);

    /// <summary>
    /// Egy edzés a mai naphoz képest megadott napon.
    /// </summary>
    private static WorkoutRecord Workout(int dayOffset, int rpe, WorkoutType type = WorkoutType.Easy) => new()
    {
        Id = Guid.NewGuid(),
        Date = Today.AddDays(dayOffset),
        Type = type,
        DistanceKm = 10,
        DurationSeconds = 3000,
        Rpe = rpe,
        Load = rpe * 50,
    };

    /// <summary>
    /// Egy mai, tervezett állapotú edzés.
    /// </summary>
    private static PlannedWorkoutRecord Planned(WorkoutType type, int targetRpe, decimal? distanceKm = null, int? durationMinutes = null) => new()
    {
        Id = Guid.NewGuid(),
        Date = Today,
        Type = type,
        PlannedDistanceKm = distanceKm,
        PlannedDurationSeconds = durationMinutes * 60,
        TargetRpe = targetRpe,
        Status = PlannedWorkoutStatus.Planned,
    };
}
