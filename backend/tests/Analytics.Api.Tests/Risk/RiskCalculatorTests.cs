using Analytics.Api.ReadModels;
using Analytics.Api.Risk;
using Contracts;

namespace Analytics.Api.Tests.Risk;

/// <summary>
/// A szabályalapú kockázati pontszám tesztjei (spec 3.8): mutatók, egyes szabályok, összeg, kategóriák.
/// </summary>
public class RiskCalculatorTests
{
    private static readonly DateOnly Today = new(2026, 11, 18);
    private static readonly DateOnly LongAgo = Today.AddDays(-60);

    /// <summary>Előzmény nélkül nincs ACWR, így az egy-egy szabályt vizsgáló tesztekbe nem szól bele.</summary>
    private static readonly DateOnly? NoHistory = null;

    /// <summary>
    /// Akut terhelés: az utolsó 7 nap összege; krónikus: az utolsó 28 nap összegének negyede.
    /// A 28 napos ablakon kívüli edzés nem számít.
    /// </summary>
    [Fact]
    public void AcuteAndChronicLoad()
    {
        var workouts = new[]
        {
            Workout(0, load: 300),
            Workout(-6, load: 100),
            Workout(-7, load: 200),
            Workout(-27, load: 400),
            Workout(-28, load: 999),
        };

        var result = RiskCalculator.Calculate(workouts, 0, Today, LongAgo);

        Assert.Equal(400, result.AcuteLoad);
        Assert.Equal(250, result.ChronicLoad);
        Assert.Equal(1.6, result.Acwr);
    }

    /// <summary>
    /// 28 napnál rövidebb előzménynél nincs ACWR, és ACWR-szabály sem teljesül.
    /// </summary>
    [Fact]
    public void Acwr_RequiresFullHistory()
    {
        var workouts = new[] { Workout(0, load: 500) };

        var result = RiskCalculator.Calculate(workouts, 0, Today, Today.AddDays(-10));

        Assert.Null(result.Acwr);
        Assert.Equal(0, result.Score);
    }

    /// <summary>
    /// ACWR &gt; 1,5: 40 pont; 1,3–1,5 (mindkét vége inkluzív): 20 pont; alatta 0.
    /// </summary>
    [Theory]
    [InlineData(1.51, 40)]
    [InlineData(1.5, 20)]
    [InlineData(1.3, 20)]
    [InlineData(1.29, 0)]
    public void AcwrPoints(double acwr, int expectedScore)
    {
        // Krónikus terhelés 100/hét: az utolsó 28 napban 400 egység, ebből az utolsó 7 napban acwr × 100.
        var acute = acwr * 100;
        var workouts = new[] { Workout(0, load: acute), Workout(-20, load: 400 - acute) };

        var result = RiskCalculator.Calculate(workouts, 0, Today, LongAgo);

        Assert.Equal(acwr, result.Acwr);
        Assert.Equal(expectedScore, result.Score);
    }

    /// <summary>
    /// Az indoklás magyar formátumú számot tartalmaz, pl. „ACWR 1,62 > 1,5”.
    /// </summary>
    [Fact]
    public void AcwrReason_UsesHungarianFormat()
    {
        var workouts = new[] { Workout(0, load: 162), Workout(-20, load: 238) };

        var result = RiskCalculator.Calculate(workouts, 0, Today, LongAgo);

        Assert.Equal(["ACWR 1,62 > 1,5"], result.Reasons);
    }

    /// <summary>
    /// A heti táv több mint 10%-os növekedése az előző héthez képest 20 pont; pontosan 10% még nem.
    /// Előző heti 0 km-hez képest nem számolunk növekedést.
    /// </summary>
    [Theory]
    [InlineData(20, 22, 0)]
    [InlineData(20, 22.1, 20)]
    [InlineData(0, 30, 0)]
    public void WeeklyKmIncrease(double previousKm, double currentKm, int expectedScore)
    {
        var workouts = new List<WorkoutRecord> { Workout(0, km: currentKm) };
        if (previousKm > 0)
        {
            workouts.Add(Workout(-7, km: previousKm));
        }

        var result = RiskCalculator.Calculate(workouts, 0, Today, NoHistory);

        Assert.Equal(expectedScore, result.Score);
        Assert.Equal((decimal)currentKm, result.WeeklyKm);
    }

    /// <summary>
    /// Két egymást követő intenzív (RPE ≥ 7) nap az utolsó 7 napban 20 pont; ha közte pihenő van, nem.
    /// </summary>
    [Theory]
    [InlineData(-3, -2, 7, 20)]
    [InlineData(-3, -1, 9, 0)]
    [InlineData(-3, -2, 6, 0)]
    [InlineData(-8, -7, 8, 0)]
    public void ConsecutiveIntenseDays(int firstDay, int secondDay, int rpe, int expectedScore)
    {
        var workouts = new[] { Workout(firstDay, rpe: rpe), Workout(secondDay, rpe: rpe) };

        var result = RiskCalculator.Calculate(workouts, 0, Today, NoHistory);

        Assert.Equal(expectedScore, result.Score);
    }

    /// <summary>
    /// Ha az utolsó 7 nap mindegyikén volt edzés, 10 pont; egy pihenőnap elég, hogy ne teljesüljön.
    /// </summary>
    [Theory]
    [InlineData(7, 10)]
    [InlineData(6, 0)]
    public void NoRestDay(int trainingDays, int expectedScore)
    {
        var workouts = Enumerable.Range(0, trainingDays).Select(day => Workout(-day)).ToList();

        var result = RiskCalculator.Calculate(workouts, 0, Today, NoHistory);

        Assert.Equal(expectedScore, result.Score);
    }

    /// <summary>
    /// Legalább 2 kihagyott tervezett edzés az utolsó 7 napban 10 pont.
    /// </summary>
    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 10)]
    [InlineData(5, 10)]
    public void MissedWorkouts(int missed, int expectedScore)
    {
        var result = RiskCalculator.Calculate([], missed, Today, null);

        Assert.Equal(expectedScore, result.Score);
    }

    /// <summary>
    /// Minden szabály egyszerre: 40 + 20 + 20 + 10 + 10 = 100, magas kategória, öt indoklás.
    /// </summary>
    [Fact]
    public void AllRules_SumToMaximum()
    {
        // 7 egymást követő intenzív edzésnap 100 terheléssel és 10 km-rel, előtte egy 5 km-es hét.
        var workouts = Enumerable.Range(0, 7).Select(day => Workout(-day, load: 100, km: 10, rpe: 8)).ToList();
        workouts.Add(Workout(-10, load: 50, km: 5));

        var result = RiskCalculator.Calculate(workouts, 2, Today, LongAgo);

        Assert.Equal(100, result.Score);
        Assert.Equal(RiskCategory.High, result.Category);
        Assert.Equal(5, result.Reasons.Count);
    }

    /// <summary>
    /// Kategóriák: 0–29 alacsony, 30–59 közepes, 60–100 magas.
    /// </summary>
    [Theory]
    [InlineData(0, RiskCategory.Low)]
    [InlineData(29, RiskCategory.Low)]
    [InlineData(30, RiskCategory.Medium)]
    [InlineData(59, RiskCategory.Medium)]
    [InlineData(60, RiskCategory.High)]
    [InlineData(100, RiskCategory.High)]
    public void Categories(int score, RiskCategory expected)
    {
        Assert.Equal(expected, RiskCalculator.CategoryFor(score));
    }

    /// <summary>
    /// Egy tesztedzés a mai naphoz képest megadott napon.
    /// </summary>
    private static WorkoutRecord Workout(int dayOffset, double load = 50, double km = 5, int rpe = 4) => new()
    {
        Id = Guid.NewGuid(),
        RunnerId = Guid.Empty,
        Date = Today.AddDays(dayOffset),
        Type = WorkoutType.Easy,
        DistanceKm = (decimal)km,
        DurationSeconds = 1800,
        Rpe = rpe,
        Load = load,
    };
}
