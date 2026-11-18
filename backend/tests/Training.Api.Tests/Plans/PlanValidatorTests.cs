using Contracts;
using Training.Api.Plans;

namespace Training.Api.Tests.Plans;

/// <summary>
/// A terv és a tervezett edzés validációjának tesztjei (spec 3.4).
/// </summary>
public class PlanValidatorTests
{
    private static readonly DateOnly PlanStart = new(2026, 11, 23);
    private static readonly DateOnly PlanEnd = new(2026, 12, 20);

    /// <summary>
    /// Érvényes terv átmegy, a név szélső szóközei levágódnak.
    /// </summary>
    [Fact]
    public void ValidPlan_Passes()
    {
        var isValid = PlanValidator.TryValidatePlan(
            new PlanRequest(null, "  Alapozás  ", PlanStart, PlanEnd, true), out var input, out var errors);

        Assert.True(isValid);
        Assert.Empty(errors);
        Assert.Equal("Alapozás", input.Name);
        Assert.True(input.LinkActiveGoal);
    }

    /// <summary>
    /// A név, a kezdő- és a záródátum kötelező.
    /// </summary>
    [Fact]
    public void Plan_MissingFields_AreReported()
    {
        PlanValidator.TryValidatePlan(new PlanRequest(null, " ", null, null, false), out _, out var errors);

        Assert.Equal(["endDate", "name", "startDate"], errors.Keys.Order());
    }

    /// <summary>
    /// A záródátum nem lehet korábbi a kezdőnél, és a terv legfeljebb egy évre szólhat.
    /// </summary>
    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(366, true)]
    [InlineData(367, false)]
    public void Plan_EndDateRules(int lengthDays, bool expectedValid)
    {
        var request = new PlanRequest(null, "Terv", PlanStart, PlanStart.AddDays(lengthDays), false);

        var isValid = PlanValidator.TryValidatePlan(request, out _, out var errors);

        Assert.Equal(expectedValid, isValid);
        Assert.Equal(!expectedValid, errors.ContainsKey("endDate"));
    }

    /// <summary>
    /// Érvényes tervezett edzés átmegy, az időtartam másodpercekre alakul.
    /// </summary>
    [Fact]
    public void ValidPlannedWorkout_Passes()
    {
        var request = new PlannedWorkoutRequest(PlanStart.AddDays(2), WorkoutType.Interval, null, new TimeSpan(0, 50, 0), 8, "6×800 m");

        var isValid = PlanValidator.TryValidatePlannedWorkout(request, PlanStart, PlanEnd, out var input, out _);

        Assert.True(isValid);
        Assert.Equal(3000, input.PlannedDurationSeconds);
        Assert.Null(input.PlannedDistanceKm);
    }

    /// <summary>
    /// A dátumnak a terv időszakába kell esnie (mindkét vége inkluzív).
    /// </summary>
    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(27, true)]
    [InlineData(28, false)]
    public void PlannedWorkout_DateMustBeInPlan(int daysFromStart, bool expectedValid)
    {
        var request = ValidPlannedWorkout() with { Date = PlanStart.AddDays(daysFromStart) };

        AssertPlannedWorkoutValidity(request, expectedValid, "date");
    }

    /// <summary>
    /// A tervezett táv és időtartam közül legalább az egyik kell.
    /// </summary>
    [Fact]
    public void PlannedWorkout_NeedsDistanceOrDuration()
    {
        var request = ValidPlannedWorkout() with { PlannedDistanceKm = null, PlannedDuration = null };

        AssertPlannedWorkoutValidity(request, false, "plannedDistanceKm");
    }

    /// <summary>
    /// A táv, időtartam és cél RPE határai ugyanazok, mint a rögzített edzésnél.
    /// </summary>
    [Fact]
    public void PlannedWorkout_RangeRules()
    {
        AssertPlannedWorkoutValidity(ValidPlannedWorkout() with { PlannedDistanceKm = 100.01m }, false, "plannedDistanceKm");
        AssertPlannedWorkoutValidity(ValidPlannedWorkout() with { PlannedDuration = TimeSpan.FromSeconds(59) }, false, "plannedDuration");
        AssertPlannedWorkoutValidity(ValidPlannedWorkout() with { TargetRpe = 11 }, false, "targetRpe");
        AssertPlannedWorkoutValidity(ValidPlannedWorkout() with { TargetRpe = null }, false, "targetRpe");
        AssertPlannedWorkoutValidity(ValidPlannedWorkout() with { Note = new string('a', 501) }, false, "note");
    }

    /// <summary>
    /// Egy minden mezőjében érvényes tervezett edzés, amiből a tesztek egy-egy mezőt rontanak el.
    /// </summary>
    private static PlannedWorkoutRequest ValidPlannedWorkout() =>
        new(PlanStart, WorkoutType.Easy, 8m, null, 3, null);

    /// <summary>
    /// Ellenőrzi, hogy a tervezett edzés az elvárásnak megfelelően érvényes-e, és hiba esetén az adott mezőnél jelez-e.
    /// </summary>
    private static void AssertPlannedWorkoutValidity(PlannedWorkoutRequest request, bool expectedValid, string field)
    {
        var isValid = PlanValidator.TryValidatePlannedWorkout(request, PlanStart, PlanEnd, out _, out var errors);

        Assert.Equal(expectedValid, isValid);
        Assert.Equal(!expectedValid, errors.ContainsKey(field));
    }
}
