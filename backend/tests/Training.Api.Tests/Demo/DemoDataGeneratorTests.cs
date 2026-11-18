using Contracts;
using Contracts.Demo;
using Training.Api.Demo;
using Training.Api.Workouts;

namespace Training.Api.Tests.Demo;

/// <summary>
/// A demóadat-generátor tesztjei: érvényes adat, 12 hét, és a négy minta jellemzői (spec 3.2).
/// </summary>
public class DemoDataGeneratorTests
{
    private static readonly DateOnly Today = new(2026, 11, 18);

    /// <summary>
    /// Minden mintánál: 12 hét múlt + 2 hét jövő terv, az edzések csak múltbeliek, és mind átmenne
    /// a rögzítés validációján.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllPatterns))]
    public void GeneratesValidTwelveWeeks(DemoRunnerPattern pattern)
    {
        var data = DemoDataGenerator.Generate(pattern, Today);
        var workouts = data.PlannedWorkouts.Where(p => p.Workout is not null).Select(p => p.Workout!).ToList();

        Assert.Equal(DemoDataGenerator.WeekStart(Today).AddDays(-77), data.Plan.StartDate);
        Assert.Equal(Today.AddDays(13), data.Plan.EndDate);
        Assert.All(data.PlannedWorkouts, p => Assert.InRange(p.Planned.Date, data.Plan.StartDate, data.Plan.EndDate));
        Assert.All(workouts, w => Assert.True(w.Date < Today));
        Assert.True(workouts.Count >= 40, $"{workouts.Count} edzés");
        Assert.All(workouts, w =>
        {
            var request = new WorkoutRequest(w.Date, w.Type, w.DistanceKm, TimeSpan.FromSeconds(w.DurationSeconds), w.AvgHeartRate, w.ElevationGainM, w.Rpe, null);
            Assert.True(WorkoutValidator.TryValidate(request, Today, out _, out var errors), string.Join("; ", errors.Keys));
        });
        Assert.All(data.PlannedWorkouts.Where(p => p.Planned.Date >= Today), p => Assert.Null(p.Workout));
    }

    /// <summary>
    /// Ugyanarra a napra ugyanazt adja (rögzített véletlenmag).
    /// </summary>
    [Fact]
    public void IsDeterministic()
    {
        var first = DemoDataGenerator.Generate(DemoRunnerPattern.FrequentSkipping, Today);
        var second = DemoDataGenerator.Generate(DemoRunnerPattern.FrequentSkipping, Today);

        Assert.Equal(first.PlannedWorkouts, second.PlannedWorkouts);
    }

    /// <summary>
    /// Egyenletes építkezés: nincs kihagyás, és a heti táv hétről hétre nő.
    /// </summary>
    [Fact]
    public void SteadyBuild_GrowsWithoutMisses()
    {
        var data = DemoDataGenerator.Generate(DemoRunnerPattern.SteadyBuild, Today);

        Assert.DoesNotContain(data.PlannedWorkouts, p => p.Missed);
        Assert.True(WeekKm(data, 10) > WeekKm(data, 1) * 1.3m);
    }

    /// <summary>
    /// Hirtelen terhelésugrás: az utolsó 7 nap távja több mint kétszerese az előző 7 napénak.
    /// </summary>
    [Fact]
    public void SuddenSpike_LastSevenDaysJump()
    {
        var data = DemoDataGenerator.Generate(DemoRunnerPattern.SuddenSpike, Today);

        Assert.True(KmBetween(data, Today.AddDays(-6), Today) > 2 * KmBetween(data, Today.AddDays(-13), Today.AddDays(-7)));
    }

    /// <summary>
    /// Gyakori kihagyás: az utolsó 7 napban legalább 2 kihagyott, és összesen legalább a negyede kimarad.
    /// </summary>
    [Fact]
    public void FrequentSkipping_MissesOften()
    {
        var past = DemoDataGenerator.Generate(DemoRunnerPattern.FrequentSkipping, Today).PlannedWorkouts.Where(p => p.Planned.Date < Today).ToList();

        Assert.True(past.Count(p => p.Missed && p.Planned.Date >= Today.AddDays(-6)) >= 2);
        Assert.True(past.Count(p => p.Missed) * 4 >= past.Count);
    }

    /// <summary>
    /// Túl sok intenzív nap: a hét bármelyik napján nézve az utolsó 7 napban két egymást követő
    /// intenzív nap, és legalább 3 intenzív nap.
    /// </summary>
    [Theory]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(18)]
    [InlineData(19)]
    [InlineData(20)]
    [InlineData(21)]
    [InlineData(22)]
    public void TooManyIntenseDays_HasConsecutiveIntenseDays(int dayOfMonth)
    {
        var today = new DateOnly(2026, 11, dayOfMonth);
        var intenseDays = DemoDataGenerator.Generate(DemoRunnerPattern.TooManyIntenseDays, today).PlannedWorkouts
            .Select(p => p.Workout)
            .Where(w => w is not null && w.Rpe >= 7 && w.Date >= today.AddDays(-6))
            .Select(w => w!.Date)
            .ToHashSet();

        Assert.True(intenseDays.Count >= 3);
        Assert.Contains(intenseDays, day => intenseDays.Contains(day.AddDays(1)));
    }

    /// <summary>A négy minta a Theory-hez.</summary>
    public static TheoryData<DemoRunnerPattern> AllPatterns => new(Enum.GetValues<DemoRunnerPattern>());

    /// <summary>
    /// Az n-edik hét (0 = a legrégebbi) elvégzett edzéseinek távja.
    /// </summary>
    private static decimal WeekKm(DemoData data, int week)
    {
        var start = data.Plan.StartDate.AddDays(7 * week);
        return KmBetween(data, start, start.AddDays(6));
    }

    /// <summary>
    /// Az elvégzett edzések távja a megadott napok között (mindkét vége inkluzív).
    /// </summary>
    private static decimal KmBetween(DemoData data, DateOnly from, DateOnly to) =>
        data.PlannedWorkouts.Where(p => p.Workout is { } w && w.Date >= from && w.Date <= to).Sum(p => p.Workout!.DistanceKm);
}
