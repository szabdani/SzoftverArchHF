using Analytics.Api.Dashboard;
using Analytics.Api.ReadModels;
using Analytics.Api.Risk;
using Contracts;
using Contracts.Training;

namespace Analytics.Api.Tests.Dashboard;

/// <summary>
/// A terhelési dashboard számításainak tesztjei (spec 3.5).
/// </summary>
public class DashboardCalculatorTests
{
    /// <summary>2026.11.18. szerda; a hét hétfőn (11.16.) kezdődik.</summary>
    private static readonly DateOnly Today = new(2026, 11, 18);
    private static readonly DateOnly Monday = new(2026, 11, 16);

    /// <summary>
    /// Heti összesítő: csak a hétfőtől ma tartó időszak; táv, idő, emelkedés, darab, átlagtempó, aktuális kockázat.
    /// </summary>
    [Fact]
    public void WeeklySummary()
    {
        var workouts = new[]
        {
            Workout(Monday, km: 10, minutes: 50, rpe: 4, elevation: 100),
            Workout(Today, km: 5, minutes: 25, rpe: 8, elevation: null),
            Workout(Monday.AddDays(-1), km: 20, minutes: 120, rpe: 5, elevation: 300),
        };
        var risk = new RiskScoreSnapshot { Score = 40, Category = RiskCategory.Medium };

        var result = DashboardCalculator.Calculate(DashboardPeriod.Week, workouts, [], Today, null, risk);

        Assert.Equal(new DashboardSummary(Monday, Today, 15, 4500, 100, 2, 300, 40, RiskCategory.Medium), result.Summary);
    }

    /// <summary>
    /// Havi összesítő: a hónap elejétől; edzés nélkül nincs átlagtempó.
    /// </summary>
    [Fact]
    public void MonthlySummary()
    {
        var workouts = new[] { Workout(new DateOnly(2026, 11, 1), km: 8, minutes: 40), Workout(new DateOnly(2026, 10, 31), km: 8, minutes: 40) };

        var month = DashboardCalculator.Calculate(DashboardPeriod.Month, workouts, [], Today, null, null).Summary;
        var emptyWeek = DashboardCalculator.Calculate(DashboardPeriod.Week, workouts, [], Today, null, null).Summary;

        Assert.Equal((new DateOnly(2026, 11, 1), 8m, 1), (month.From, month.DistanceKm, month.WorkoutCount));
        Assert.Null(emptyWeek.AvgPaceSecondsPerKm);
        Assert.Null(emptyWeek.RiskScore);
    }

    /// <summary>
    /// Intenzitáseloszlás az időszakra, percben: RPE 1–4 könnyű, 5–6 közepes, 7–10 intenzív.
    /// </summary>
    [Fact]
    public void IntensityDistribution()
    {
        var workouts = new[]
        {
            Workout(Monday, minutes: 40, rpe: 3),
            Workout(Monday.AddDays(1), minutes: 30, rpe: 6),
            Workout(Today, minutes: 20, rpe: 9),
            Workout(Today, minutes: 15, rpe: 4),
        };

        var result = DashboardCalculator.Calculate(DashboardPeriod.Week, workouts, [], Today, null, null);

        Assert.Equal(new IntensityDistribution(55, 30, 20), result.Intensity);
    }

    /// <summary>
    /// 12 hét, a legrégebbi elöl, az utolsó a folyó hét; a heti km és terhelés hétfőtől vasárnapig összegződik.
    /// </summary>
    [Fact]
    public void Weeks_AreTwelveMondaysWithTotals()
    {
        var workouts = new[]
        {
            Workout(Monday.AddDays(-7), km: 10, load: 200),
            Workout(Monday.AddDays(-1), km: 12, load: 300),
            Workout(Today, km: 5, load: 100),
        };

        var weeks = DashboardCalculator.Calculate(DashboardPeriod.Week, workouts, [], Today, null, null).Weeks;

        Assert.Equal(12, weeks.Count);
        Assert.Equal(Monday.AddDays(-77), weeks[0].WeekStart);
        Assert.Equal(Monday, weeks[^1].WeekStart);
        Assert.Equal((22m, 500.0, 2), (weeks[^2].DistanceKm, weeks[^2].Load, weeks[^2].WorkoutCount));
        Assert.Equal((5m, 100.0), (weeks[^1].DistanceKm, weeks[^1].Load));
    }

    /// <summary>
    /// A heti ACWR és pontszám a hét végére számolódik: egy egyenletes alap után megugró hétnél magas.
    /// </summary>
    [Fact]
    public void Weeks_AcwrAndRiskAtWeekEnd()
    {
        // Hat hét egyenletes alap (heti 300 terhelés), majd az előző héten háromszoros terhelés.
        var workouts = Enumerable.Range(2, 6)
            .Select(weeksAgo => Workout(Monday.AddDays(-7 * weeksAgo + 2), km: 20, load: 300))
            .Append(Workout(Monday.AddDays(-5), km: 20, load: 900))
            .ToList();

        var weeks = DashboardCalculator.Calculate(DashboardPeriod.Week, workouts, [], Today, Monday.AddDays(-60), null).Weeks;

        // Az előző hét vége (vasárnap): akut 900, krónikus (300 + 300 + 300 + 900) / 4 = 450 → ACWR 2.
        Assert.Equal(2.0, weeks[^2].Acwr);
        Assert.Equal(40, weeks[^2].RiskScore);
        Assert.Equal(1.0, weeks[^3].Acwr);
        Assert.Equal(0, weeks[^3].RiskScore);
    }

    /// <summary>
    /// Terv és tény: heti tervezett, teljesített és kihagyott darabszám.
    /// </summary>
    [Fact]
    public void Weeks_PlanVersusActual()
    {
        var planned = new[]
        {
            Planned(Monday.AddDays(-7), PlannedWorkoutStatus.Completed),
            Planned(Monday.AddDays(-5), PlannedWorkoutStatus.Missed),
            Planned(Monday.AddDays(-3), PlannedWorkoutStatus.Completed),
            Planned(Today.AddDays(2), PlannedWorkoutStatus.Planned),
        };

        var weeks = DashboardCalculator.Calculate(DashboardPeriod.Week, [], planned, Today, null, null).Weeks;

        Assert.Equal((3, 2, 1), (weeks[^2].PlannedCount, weeks[^2].CompletedCount, weeks[^2].MissedCount));
        Assert.Equal((1, 0, 0), (weeks[^1].PlannedCount, weeks[^1].CompletedCount, weeks[^1].MissedCount));
    }

    /// <summary>
    /// A hét hétfőn kezdődik: vasárnap az előző hétfő, hétfőn önmaga.
    /// </summary>
    [Theory]
    [InlineData(2026, 11, 22, 16)]
    [InlineData(2026, 11, 16, 16)]
    [InlineData(2026, 11, 18, 16)]
    public void WeekStart_IsMonday(int year, int month, int day, int expectedDay)
    {
        Assert.Equal(new DateOnly(2026, 11, expectedDay), DashboardCalculator.WeekStart(new DateOnly(year, month, day)));
    }

    /// <summary>
    /// Egy tesztedzés.
    /// </summary>
    private static WorkoutRecord Workout(DateOnly date, decimal km = 5, int minutes = 30, int rpe = 4, int? elevation = null, double load = 100) => new()
    {
        Id = Guid.NewGuid(),
        Date = date,
        Type = WorkoutType.Easy,
        DistanceKm = km,
        DurationSeconds = minutes * 60,
        Rpe = rpe,
        ElevationGainM = elevation,
        Load = load,
    };

    /// <summary>
    /// Egy tervezett edzés a megadott állapottal.
    /// </summary>
    private static PlannedWorkoutRecord Planned(DateOnly date, PlannedWorkoutStatus status) => new()
    {
        Id = Guid.NewGuid(),
        Date = date,
        Type = WorkoutType.Easy,
        TargetRpe = 3,
        Status = status,
    };
}
