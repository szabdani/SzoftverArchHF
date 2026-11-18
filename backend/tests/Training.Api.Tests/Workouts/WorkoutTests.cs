using Contracts;
using Training.Api.Workouts;

namespace Training.Api.Tests.Workouts;

/// <summary>
/// Az edzés számított értékeinek (terhelés, tempó) tesztjei.
/// </summary>
public class WorkoutTests
{
    /// <summary>
    /// sRPE = RPE × időtartam percben, egy tizedesre kerekítve.
    /// </summary>
    [Theory]
    [InlineData(6, 2730, 273.0)]
    [InlineData(7, 3600, 420.0)]
    [InlineData(5, 61, 5.1)]
    public void CalculateLoad_IsRpeTimesMinutes(int rpe, int durationSeconds, double expectedLoad)
    {
        Assert.Equal(expectedLoad, Workout.CalculateLoad(rpe, durationSeconds));
    }

    /// <summary>
    /// Tempó = időtartam / táv, másodperc/km-ben.
    /// </summary>
    [Theory]
    [InlineData(1500, 5.0, 300)]
    [InlineData(2730, 10.0, 273)]
    [InlineData(3000, 7.5, 400)]
    public void CalculatePace_IsDurationPerKm(int durationSeconds, double distanceKm, int expectedPace)
    {
        Assert.Equal(expectedPace, Workout.CalculatePaceSecondsPerKm(durationSeconds, (decimal)distanceKm));
    }

    /// <summary>
    /// Létrehozáskor kiszámolja a terhelést, és levágja a megjegyzés szélén lévő szóközöket.
    /// </summary>
    [Fact]
    public void Create_SetsLoadAndTrimsNote()
    {
        var input = new ValidWorkoutInput(new DateOnly(2026, 11, 17), WorkoutType.Tempo, 10m, 2730, null, null, 6, "  tempó  ");

        var workout = Workout.Create(Guid.NewGuid(), input, DateTimeOffset.UtcNow);

        Assert.Equal(273.0, workout.Load);
        Assert.Equal("tempó", workout.Note);
        Assert.Equal(273, workout.PaceSecondsPerKm);
    }

    /// <summary>
    /// A csak szóközökből álló megjegyzés üresnek (null) számít.
    /// </summary>
    [Fact]
    public void Create_WhitespaceNote_BecomesNull()
    {
        var input = new ValidWorkoutInput(new DateOnly(2026, 11, 17), WorkoutType.Easy, 5m, 1800, null, null, 3, "   ");

        var workout = Workout.Create(Guid.NewGuid(), input, DateTimeOffset.UtcNow);

        Assert.Null(workout.Note);
    }
}
