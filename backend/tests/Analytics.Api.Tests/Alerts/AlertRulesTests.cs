using Analytics.Api.Alerts;
using Analytics.Api.ReadModels;
using Analytics.Api.Risk;
using Contracts;
using Contracts.Training;

namespace Analytics.Api.Tests.Alerts;

/// <summary>
/// A figyelmeztetési szabályok tesztjei (spec 3.6): súlyosság, üzenet, időszak.
/// </summary>
public class AlertRulesTests
{
    private static readonly DateOnly Today = new(2026, 11, 18);

    /// <summary>
    /// ACWR &gt; 1,5: kritikus „hirtelen terhelésnövekedés”; az ACWR emelkedő sávja ilyenkor nem jön külön.
    /// </summary>
    [Fact]
    public void AcwrAboveCritical_IsCritical()
    {
        var candidates = AlertRules.Evaluate(Result(acwr: 1.62, weeklyKm: 20, previousKm: 20), [], Today);

        var alert = Assert.Single(candidates);
        Assert.Equal(AlertRule.SuddenLoadIncrease, alert.Rule);
        Assert.Equal(AlertSeverity.Critical, alert.Severity);
        Assert.Equal("Hirtelen terhelésnövekedés: ACWR 1,62 > 1,5", alert.Message);
        Assert.Equal("2026-W47", alert.PeriodKey);
    }

    /// <summary>
    /// Emelkedő terhelés: ACWR 1,3–1,5 vagy több mint 10%-os heti km-növekedés; egy figyelmeztetés, mindkét okkal.
    /// </summary>
    [Fact]
    public void RisingLoad_CombinesReasons()
    {
        var candidates = AlertRules.Evaluate(Result(acwr: 1.4, weeklyKm: 30, previousKm: 25), [], Today);

        var alert = Assert.Single(candidates);
        Assert.Equal(AlertRule.RisingLoad, alert.Rule);
        Assert.Equal(AlertSeverity.Warning, alert.Severity);
        Assert.Equal("Emelkedő terhelés: ACWR 1,40 az 1,3–1,5 sávban, a heti táv 20%-kal nőtt", alert.Message);
    }

    /// <summary>
    /// Kritikus ACWR mellett a több mint 10%-os heti km-növekedés külön emelkedő terhelés figyelmeztetést ad.
    /// </summary>
    [Fact]
    public void CriticalAcwrAndWeeklyKmIncrease_GiveBothAlerts()
    {
        var candidates = AlertRules.Evaluate(Result(acwr: 1.62, weeklyKm: 40, previousKm: 20), [], Today);

        Assert.Equal([AlertRule.SuddenLoadIncrease, AlertRule.RisingLoad], candidates.Select(c => c.Rule));
        Assert.Equal("Emelkedő terhelés: a heti táv 100%-kal nőtt", candidates[1].Message);
    }

    /// <summary>
    /// Túl sűrű intenzív napok: két egymást követő, vagy legalább 3 intenzív nap 7 napon belül.
    /// </summary>
    [Theory]
    [InlineData(true, 2, true)]
    [InlineData(false, 3, true)]
    [InlineData(false, 2, false)]
    public void DenseIntenseDays(bool consecutive, int intenseDays, bool expected)
    {
        var result = Result() with
        {
            ConsecutiveIntenseStart = consecutive ? Today.AddDays(-2) : null,
            IntenseDaysLast7 = intenseDays,
        };

        var candidates = AlertRules.Evaluate(result, [], Today);

        Assert.Equal(expected, candidates.Any(c => c.Rule == AlertRule.DenseIntenseDays && c.Severity == AlertSeverity.Warning));
    }

    /// <summary>
    /// Minden kihagyott edzésről információ (a tervezett edzés az időszak), kettőtől heti figyelmeztetés is.
    /// </summary>
    [Fact]
    public void MissedWorkouts()
    {
        var first = Missed(-3, WorkoutType.Tempo);
        var second = Missed(-1, WorkoutType.Long);

        var one = AlertRules.Evaluate(Result(), [first], Today);
        var two = AlertRules.Evaluate(Result(), [first, second], Today);

        var info = Assert.Single(one);
        Assert.Equal(AlertSeverity.Info, info.Severity);
        Assert.Equal("Kihagyott edzés: 11.15. (tempó)", info.Message);
        Assert.Equal($"planned:{first.Id}", info.PeriodKey);
        Assert.Equal(3, two.Count);
        Assert.Contains(two, c => c.Rule == AlertRule.FrequentMissedWorkouts && c.Severity == AlertSeverity.Warning && c.PeriodKey == "2026-W47");
    }

    /// <summary>
    /// Pihenőnap hiánya: információ.
    /// </summary>
    [Fact]
    public void NoRestDay_IsInfo()
    {
        var alert = Assert.Single(AlertRules.Evaluate(Result() with { NoRestDay = true }, [], Today));

        Assert.Equal(AlertRule.NoRestDay, alert.Rule);
        Assert.Equal(AlertSeverity.Info, alert.Severity);
    }

    /// <summary>
    /// Ha egy szabály sem teljesül, nincs figyelmeztetés.
    /// </summary>
    [Fact]
    public void NothingTriggered_NoAlerts()
    {
        Assert.Empty(AlertRules.Evaluate(Result(acwr: 1.1, weeklyKm: 21, previousKm: 20), [], Today));
    }

    /// <summary>
    /// Az ISO hét kulcsa az év elején a megelőző év utolsó hetére is eshet.
    /// </summary>
    [Theory]
    [InlineData(2026, 11, 18, "2026-W47")]
    [InlineData(2027, 1, 1, "2026-W53")]
    [InlineData(2027, 1, 4, "2027-W01")]
    public void IsoWeekKey(int year, int month, int day, string expected)
    {
        Assert.Equal(expected, AlertRules.IsoWeekKey(new DateOnly(year, month, day)));
    }

    /// <summary>
    /// Egy kockázati eredmény a megadott mutatókkal, minden más szabály nélkül.
    /// </summary>
    private static RiskResult Result(double? acwr = null, decimal weeklyKm = 0, decimal previousKm = 0) =>
        new(0, 0, acwr, weeklyKm, previousKm, 0, RiskCategory.Low, [], 0, null, false);

    /// <summary>
    /// Egy kihagyott tervezett edzés a mai naphoz képest megadott napon.
    /// </summary>
    private static PlannedWorkoutRecord Missed(int dayOffset, WorkoutType type) => new()
    {
        Id = Guid.NewGuid(),
        Date = Today.AddDays(dayOffset),
        Type = type,
        TargetRpe = 7,
        Status = PlannedWorkoutStatus.Missed,
    };
}
