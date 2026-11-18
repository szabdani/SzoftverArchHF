using Analytics.Api.Alerts;
using Analytics.Api.ReadModels;
using Analytics.Api.Risk;
using Analytics.Api.Rules;
using Contracts;

namespace Analytics.Api.Tests.Rules;

/// <summary>
/// Az adminisztrátor által állítható küszöbök és aktív állapotok hatása (UC17), és a validáció.
/// </summary>
public class RuleSettingsTests
{
    private static readonly DateOnly Today = new(2026, 11, 18);
    private static readonly DateOnly LongAgo = Today.AddDays(-60);

    /// <summary>
    /// A spec alapértelmezései érvényes beállítást adnak.
    /// </summary>
    [Fact]
    public void Defaults_AreValid()
    {
        Assert.True(RuleSettingsValidator.TryValidate(RuleSettingsDto.From(new RuleSettings()), out var errors));
        Assert.Empty(errors);
    }

    /// <summary>
    /// A kritikus ACWR küszöb emelésével 1,62 már csak az emelkedő sávba esik; az indoklás a beállított küszöböt mutatja.
    /// </summary>
    [Fact]
    public void RaisedCriticalThreshold_MovesAcwrToElevatedBand()
    {
        var settings = new RuleSettings { AcwrCritical = 2.0 };

        var result = RiskCalculator.Calculate(AcwrWorkouts(1.62), 0, Today, LongAgo, settings);

        Assert.Equal(20, result.Score);
        Assert.Equal(["ACWR 1,62 az 1,3–2,0 sávban"], result.Reasons);
    }

    /// <summary>
    /// A sáv névelője a küszöb kiejtéséhez igazodik: „az 1,3”, de „a 2,0”.
    /// </summary>
    [Theory]
    [InlineData(1.3, 1.5, "az 1,3–1,5 sávban")]
    [InlineData(2.0, 3.0, "a 2,0–3,0 sávban")]
    [InlineData(0.9, 1.5, "a 0,9–1,5 sávban")]
    public void ElevatedBand_UsesCorrectArticle(double elevated, double critical, string expected)
    {
        Assert.Equal(expected, RiskCalculator.ElevatedBand(new RuleSettings { AcwrElevated = elevated, AcwrCritical = critical }));
    }

    /// <summary>
    /// Kikapcsolt ACWR-szabálynál nincs pont és nincs figyelmeztetés, a mutató viszont kiszámolódik.
    /// </summary>
    [Fact]
    public void DisabledAcwrRule_NoPointsNoAlert()
    {
        var settings = new RuleSettings { AcwrEnabled = false };

        var result = RiskCalculator.Calculate(AcwrWorkouts(1.62), 0, Today, LongAgo, settings);

        Assert.Equal(1.62, result.Acwr);
        Assert.Equal(0, result.Score);
        Assert.Empty(AlertRules.Evaluate(result, [], Today, settings));
    }

    /// <summary>
    /// A kihagyott edzések küszöbe állítható: 3-as küszöbnél 2 kihagyott még nem számít.
    /// </summary>
    [Theory]
    [InlineData(2, 10)]
    [InlineData(3, 0)]
    public void MissedWorkoutsThreshold(int threshold, int expectedScore)
    {
        var result = RiskCalculator.Calculate([], 2, Today, null, new RuleSettings { MissedWorkoutsPerWeek = threshold });

        Assert.Equal(expectedScore, result.Score);
    }

    /// <summary>
    /// Az intenzív RPE küszöbe állítható: 8-as küszöbnél két egymást követő RPE 7-es nap nem számít.
    /// </summary>
    [Theory]
    [InlineData(7, 20)]
    [InlineData(8, 0)]
    public void IntenseRpeThreshold(int intenseRpe, int expectedScore)
    {
        var workouts = new[] { Workout(-2, rpe: 7), Workout(-1, rpe: 7) };

        var result = RiskCalculator.Calculate(workouts, 0, Today, null, new RuleSettings { IntenseRpe = intenseRpe });

        Assert.Equal(expectedScore, result.Score);
    }

    /// <summary>
    /// A „túl sűrű intenzív napok” heti küszöbe állítható, és kikapcsolva nincs figyelmeztetés.
    /// </summary>
    [Theory]
    [InlineData(3, true, true)]
    [InlineData(4, true, false)]
    [InlineData(3, false, false)]
    public void DenseIntenseDaysAlertSettings(int perWeek, bool enabled, bool expectedAlert)
    {
        var result = new RiskResult(0, 0, null, 0, 0, 0, RiskCategory.Low, [], 3, null, false);
        var settings = new RuleSettings { IntenseDaysPerWeek = perWeek, IntenseDaysEnabled = enabled };

        var candidates = AlertRules.Evaluate(result, [], Today, settings);

        Assert.Equal(expectedAlert, candidates.Any(c => c.Rule == AlertRule.DenseIntenseDays));
    }

    /// <summary>
    /// Érvénytelen beállítások: az emelkedő küszöb nem lehet a kritikus felett, és a tartományokon kívüli érték hibás.
    /// </summary>
    [Fact]
    public void InvalidSettings_AreReported()
    {
        var request = RuleSettingsDto.From(new RuleSettings()) with
        {
            AcwrElevated = 1.6,
            WeeklyKmIncreasePercent = 0,
            IntenseRpe = 11,
            IntenseDaysPerWeek = 1,
            MissedWorkoutsPerWeek = 8,
        };

        Assert.False(RuleSettingsValidator.TryValidate(request, out var errors));
        Assert.Equal(
            ["acwrElevated", "intenseDaysPerWeek", "intenseRpe", "missedWorkoutsPerWeek", "weeklyKmIncreasePercent"],
            errors.Keys.Order());
    }

    /// <summary>
    /// Olyan edzések, amelyeknél az ACWR pontosan a megadott érték (krónikus terhelés heti 100).
    /// </summary>
    private static List<WorkoutRecord> AcwrWorkouts(double acwr) =>
        [Workout(0, load: acwr * 100), Workout(-20, load: 400 - acwr * 100)];

    /// <summary>
    /// Egy tesztedzés a mai naphoz képest megadott napon.
    /// </summary>
    private static WorkoutRecord Workout(int dayOffset, double load = 50, int rpe = 4) => new()
    {
        Id = Guid.NewGuid(),
        Date = Today.AddDays(dayOffset),
        Type = WorkoutType.Easy,
        DistanceKm = 5,
        DurationSeconds = 1800,
        Rpe = rpe,
        Load = load,
    };
}
