using Training.Api.Goals;

namespace Training.Api.Tests.Goals;

/// <summary>
/// A cél validációjának tesztjei (spec 3.3).
/// </summary>
public class GoalValidatorTests
{
    private static readonly DateOnly Today = new(2026, 11, 18);

    /// <summary>
    /// Fix típusú célnál a táv a típusból adódik, a megadott távot figyelmen kívül hagyja.
    /// </summary>
    [Theory]
    [InlineData(GoalType.FiveK, 5.0)]
    [InlineData(GoalType.TenK, 10.0)]
    [InlineData(GoalType.HalfMarathon, 21.0975)]
    [InlineData(GoalType.Marathon, 42.195)]
    public void FixedType_UsesStandardDistance(GoalType type, double expectedDistanceKm)
    {
        var request = new GoalRequest(type, 3m, Today.AddDays(60), null);

        var isValid = GoalValidator.TryValidate(request, Today, out var input, out var errors);

        Assert.True(isValid);
        Assert.Empty(errors);
        Assert.Equal((decimal)expectedDistanceKm, input.DistanceKm);
    }

    /// <summary>
    /// Egyéni célnál a megadott táv marad, és a célidő másodpercekre alakul.
    /// </summary>
    [Fact]
    public void CustomGoal_KeepsDistanceAndTargetTime()
    {
        var request = new GoalRequest(GoalType.Custom, 15.5m, Today.AddDays(30), new TimeSpan(1, 20, 0));

        var isValid = GoalValidator.TryValidate(request, Today, out var input, out _);

        Assert.True(isValid);
        Assert.Equal(15.5m, input.DistanceKm);
        Assert.Equal(4800, input.TargetTimeSeconds);
    }

    /// <summary>
    /// Egyéni célnál kötelező a táv, és 0,1–500 km közötti, legfeljebb két tizedes lehet.
    /// </summary>
    [Theory]
    [InlineData(null, false)]
    [InlineData(0.09, false)]
    [InlineData(0.1, true)]
    [InlineData(500.0, true)]
    [InlineData(500.01, false)]
    [InlineData(12.345, false)]
    public void CustomGoal_DistanceRules(double? distanceKm, bool expectedValid)
    {
        var request = new GoalRequest(GoalType.Custom, (decimal?)distanceKm, Today.AddDays(30), null);

        AssertValidity(request, expectedValid, "distanceKm");
    }

    /// <summary>
    /// A céldátum lehet ma, de nem lehet múltbeli.
    /// </summary>
    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(100, true)]
    public void TargetDate_CannotBeInPast(int daysFromToday, bool expectedValid)
    {
        var request = new GoalRequest(GoalType.TenK, null, Today.AddDays(daysFromToday), null);

        AssertValidity(request, expectedValid, "targetDate");
    }

    /// <summary>
    /// A célidő opcionális, 1 perc és 24 óra között, egész másodpercre.
    /// </summary>
    [Theory]
    [InlineData(null, true)]
    [InlineData(59.0, false)]
    [InlineData(60.0, true)]
    [InlineData(86_400.0, true)]
    [InlineData(86_401.0, false)]
    [InlineData(90.5, false)]
    public void TargetTime_Rules(double? seconds, bool expectedValid)
    {
        TimeSpan? targetTime = seconds is { } s ? TimeSpan.FromMilliseconds(s * 1000) : null;
        var request = new GoalRequest(GoalType.Marathon, null, Today.AddDays(90), targetTime);

        AssertValidity(request, expectedValid, "targetTime");
    }

    /// <summary>
    /// A típus és a céldátum kötelező; ismeretlen típus nem mehet át.
    /// </summary>
    [Fact]
    public void MissingOrUnknownFields_AreReported()
    {
        GoalValidator.TryValidate(new GoalRequest(null, null, null, null), Today, out _, out var missing);
        GoalValidator.TryValidate(new GoalRequest((GoalType)99, null, Today, null), Today, out _, out var unknown);

        Assert.Equal(["targetDate", "type"], missing.Keys.Order());
        Assert.True(unknown.ContainsKey("type"));
    }

    /// <summary>
    /// Ellenőrzi, hogy a kérés az elvárásnak megfelelően érvényes-e, és hiba esetén az adott mezőnél jelez-e.
    /// </summary>
    private static void AssertValidity(GoalRequest request, bool expectedValid, string field)
    {
        var isValid = GoalValidator.TryValidate(request, Today, out _, out var errors);

        Assert.Equal(expectedValid, isValid);
        Assert.Equal(!expectedValid, errors.ContainsKey(field));
    }
}
