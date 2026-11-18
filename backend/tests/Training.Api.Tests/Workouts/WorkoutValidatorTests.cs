using Contracts;
using Training.Api.Workouts;

namespace Training.Api.Tests.Workouts;

/// <summary>
/// Az edzés validációjának tesztjei (spec 3.2 táblázat).
/// </summary>
public class WorkoutValidatorTests
{
    private static readonly DateOnly Today = new(2026, 11, 18);

    /// <summary>
    /// Egy minden mezőjében érvényes kérés átmegy, és az időtartam másodpercekre alakul.
    /// </summary>
    [Fact]
    public void ValidRequest_Passes()
    {
        var isValid = WorkoutValidator.TryValidate(ValidRequest(), Today, out var input, out var errors);

        Assert.True(isValid);
        Assert.Empty(errors);
        Assert.Equal(2730, input.DurationSeconds);
        Assert.Equal(WorkoutType.Easy, input.Type);
    }

    /// <summary>
    /// A mai nap még rögzíthető, a holnapi már nem.
    /// </summary>
    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void Date_CannotBeInFuture(int daysFromToday, bool expectedValid)
    {
        var request = ValidRequest() with { Date = Today.AddDays(daysFromToday) };

        AssertValidity(request, expectedValid, "date");
    }

    /// <summary>
    /// A kötelező mezők hiányát mezőnként jelzi.
    /// </summary>
    [Fact]
    public void MissingRequiredFields_AreReported()
    {
        var request = new WorkoutRequest(null, null, null, null, null, null, null, null);

        var isValid = WorkoutValidator.TryValidate(request, Today, out _, out var errors);

        Assert.False(isValid);
        Assert.Equal(["date", "distanceKm", "duration", "rpe", "type"], errors.Keys.Order());
    }

    /// <summary>
    /// Ismeretlen edzéstípus (nem definiált enum érték) nem mehet át.
    /// </summary>
    [Fact]
    public void UndefinedType_Fails()
    {
        var request = ValidRequest() with { Type = (WorkoutType)99 };

        AssertValidity(request, false, "type");
    }

    /// <summary>
    /// Táv: 0,1–100 km (a több tizedesjegy nem hiba, kerekítődik).
    /// </summary>
    [Theory]
    [InlineData(0.09, false)]
    [InlineData(0.1, true)]
    [InlineData(100, true)]
    [InlineData(100.01, false)]
    [InlineData(10.123, true)]
    public void DistanceKm_Boundaries(double distanceKm, bool expectedValid)
    {
        var request = ValidRequest() with { DistanceKm = (decimal)distanceKm };

        AssertValidity(request, expectedValid, "distanceKm");
    }

    /// <summary>
    /// A táv két tizedesre kerekítődik (a felezésnél felfelé).
    /// </summary>
    [Theory]
    [InlineData(10.123, 10.12)]
    [InlineData(10.125, 10.13)]
    [InlineData(8.5, 8.5)]
    public void DistanceKm_IsRoundedToTwoDecimals(double distanceKm, double expected)
    {
        var request = ValidRequest() with { DistanceKm = (decimal)distanceKm };

        Assert.True(WorkoutValidator.TryValidate(request, Today, out var input, out _));
        Assert.Equal((decimal)expected, input.DistanceKm);
    }

    /// <summary>
    /// Időtartam: 1 perc – 10 óra, egész másodpercre.
    /// </summary>
    [Theory]
    [InlineData(59, false)]
    [InlineData(60, true)]
    [InlineData(36_000, true)]
    [InlineData(36_001, false)]
    public void Duration_Boundaries(int seconds, bool expectedValid)
    {
        var request = ValidRequest() with { Duration = TimeSpan.FromSeconds(seconds) };

        AssertValidity(request, expectedValid, "duration");
    }

    /// <summary>
    /// Másodperc törtrésze nem adható meg (óó:pp:mm formátum).
    /// </summary>
    [Fact]
    public void Duration_WithFractionalSeconds_Fails()
    {
        var request = ValidRequest() with { Duration = TimeSpan.FromMilliseconds(90_500) };

        AssertValidity(request, false, "duration");
    }

    /// <summary>
    /// Átlagpulzus: opcionális, 40–220.
    /// </summary>
    [Theory]
    [InlineData(null, true)]
    [InlineData(39, false)]
    [InlineData(40, true)]
    [InlineData(220, true)]
    [InlineData(221, false)]
    public void AvgHeartRate_Boundaries(int? heartRate, bool expectedValid)
    {
        var request = ValidRequest() with { AvgHeartRate = heartRate };

        AssertValidity(request, expectedValid, "avgHeartRate");
    }

    /// <summary>
    /// Emelkedés: opcionális, 0–5000 m.
    /// </summary>
    [Theory]
    [InlineData(null, true)]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(5000, true)]
    [InlineData(5001, false)]
    public void ElevationGain_Boundaries(int? elevationM, bool expectedValid)
    {
        var request = ValidRequest() with { ElevationGainM = elevationM };

        AssertValidity(request, expectedValid, "elevationGainM");
    }

    /// <summary>
    /// RPE: 1–10.
    /// </summary>
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(10, true)]
    [InlineData(11, false)]
    public void Rpe_Boundaries(int rpe, bool expectedValid)
    {
        var request = ValidRequest() with { Rpe = rpe };

        AssertValidity(request, expectedValid, "rpe");
    }

    /// <summary>
    /// Megjegyzés: legfeljebb 500 karakter.
    /// </summary>
    [Theory]
    [InlineData(500, true)]
    [InlineData(501, false)]
    public void Note_MaxLength(int length, bool expectedValid)
    {
        var request = ValidRequest() with { Note = new string('a', length) };

        AssertValidity(request, expectedValid, "note");
    }

    /// <summary>
    /// Egy minden mezőjében érvényes kérés, amiből a tesztek egy-egy mezőt rontanak el.
    /// </summary>
    private static WorkoutRequest ValidRequest() =>
        new(Today, WorkoutType.Easy, 10m, new TimeSpan(0, 45, 30), 150, 120, 6, "Könnyű kör");

    /// <summary>
    /// Ellenőrzi, hogy a kérés az elvárásnak megfelelően érvényes-e, és hiba esetén az adott mezőnél jelez-e.
    /// </summary>
    private static void AssertValidity(WorkoutRequest request, bool expectedValid, string field)
    {
        var isValid = WorkoutValidator.TryValidate(request, Today, out _, out var errors);

        Assert.Equal(expectedValid, isValid);
        Assert.Equal(!expectedValid, errors.ContainsKey(field));
    }
}
